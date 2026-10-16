"""Behavioral tests run in temporary fixtures, never in the Unity asset tree."""
from collections import Counter
import base64
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / "quantum_guard.py"
spec = importlib.util.spec_from_file_location("guard", SCRIPT)
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)


class GuardTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="lsdf-guard-test-")
        self.root = Path(self.directory.name)
        self.sim = self.root / guard.SIMULATION
        self.sim.mkdir(parents=True)
        self.source = self.sim / "Fighter.cs"
        self.source.write_text("class Fighter { int health = 170; }\n", encoding="utf-8")
        self.baseline()

    def tearDown(self):
        self.directory.cleanup()

    def baseline(self):
        target = self.root / guard.BASELINE
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps({"schema": 1, "scope": guard.SIMULATION,
                          "entries": dict(Counter(f["fingerprint"] for f in guard.scan(self.root)))}),
                          encoding="utf-8")

    def test_known_debt_is_allowed_but_duplicate_is_not(self):
        self.source.write_text("int x = PlayerPrefs.GetInt(\"Skill_0\");\n", encoding="utf-8")
        self.baseline()
        self.assertEqual(guard.check(self.root)["status"], "pass")
        self.source.write_text(self.source.read_text() * 2, encoding="utf-8")
        result = guard.check(self.root)
        self.assertEqual(result["status"], "fail")
        self.assertEqual(len(result["new_findings"]), 1)

    def test_line_motion_and_comments_do_not_create_new_debt(self):
        self.source.write_text("private int currentFrame;\n", encoding="utf-8")
        self.baseline()
        self.source.write_text("// PlayerPrefs.GetInt(\"x\")\n\n  private   int currentFrame; // note\n", encoding="utf-8")
        self.assertEqual(guard.check(self.root)["status"], "pass")

    def test_fp_code_and_documentation_strings_are_not_float_math(self):
        source = 'FP value = FP._0_50; string note = "PlayerPrefs Time.deltaTime 1.2f";\n/* float value; */'
        self.assertEqual(guard.analyze_text("Fighter.cs", source), [])
        result = guard.analyze_text("Fighter.cs", 'damage = (int)(damage * 1.2f);')
        self.assertIn("Q003-floating-math", [f["rule"] for f in result])

    def test_excluded_directories_and_view_are_not_read(self):
        for excluded in guard.EXCLUDED:
            folder = self.sim / excluded
            folder.mkdir()
            (folder / "Bad.cs").write_text("PlayerPrefs.GetInt(\"x\");", encoding="utf-8")
        view = self.root / "Assets/QuantumUser/View"
        view.mkdir()
        (view / "View.cs").write_text("float t = Time.deltaTime;", encoding="utf-8")
        self.assertEqual(guard.scan(self.root), [])

    def test_removed_debt_is_reported(self):
        self.source.write_text("private bool bufferedNextAttack;\n", encoding="utf-8")
        self.baseline()
        self.source.write_text("class Fighter {}", encoding="utf-8")
        result = guard.check(self.root)
        self.assertEqual(result["resolved_baseline_occurrences"], 1)
        self.assertEqual(result["status"], "pass")

    def test_stop_continues_at_most_once(self):
        self.source.write_text("PlayerPrefs.GetInt(\"x\");", encoding="utf-8")
        first = guard.hook_response(self.root, {"hook_event_name": "Stop"})
        self.assertEqual(first["decision"], "block")
        second = guard.hook_response(self.root, {"hook_event_name": "Stop", "stop_hook_active": True})
        self.assertNotIn("decision", second)
        self.assertIn("No further automatic continuation", second["systemMessage"])

    def test_session_start_preserves_dirty_worktree_context(self):
        with patch.object(guard, "git_status", return_value=" M Fighter.cs"):
            result = guard.hook_response(self.root, {"hook_event_name": "SessionStart"})
        self.assertIn("existing changes: True", result["hookSpecificOutput"]["additionalContext"])

    def test_missing_baseline_is_error_not_pass(self):
        (self.root / guard.BASELINE).unlink()
        result = subprocess.run([sys.executable, str(SCRIPT), "check", "--root", str(self.root)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertEqual(json.loads(result.stdout)["status"], "error")

    def test_malformed_hook_input_reports_unavailable_without_loop(self):
        result = subprocess.run([sys.executable, str(SCRIPT), "hook", "--root", str(self.root)],
                                input="{invalid", capture_output=True, text=True)
        self.assertEqual(result.returncode, 0)
        self.assertIn("NOT passed", json.loads(result.stdout)["systemMessage"])

    def test_cli_is_read_only(self):
        before = {p.relative_to(self.root): p.read_bytes() for p in self.root.rglob("*") if p.is_file()}
        result = subprocess.run([sys.executable, str(SCRIPT), "check", "--root", str(self.root)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0)
        after = {p.relative_to(self.root): p.read_bytes() for p in self.root.rglob("*") if p.is_file()}
        self.assertEqual(before, after)

    def test_registered_hook_commands_from_project_subdirectory(self):
        project = SCRIPT.parents[2]
        configuration = json.loads((project / ".codex/hooks.json").read_text(encoding="utf-8"))
        for event in configuration["hooks"]:
            command = configuration["hooks"][event][0]["hooks"][0]["command"]
            # Pass the exact command as shell source, not as a second nested
            # PowerShell string which would strip Python's argument quotes.
            if sys.platform == "win32":
                encoded = base64.b64encode(command.encode("utf-16le")).decode("ascii")
                invocation = ["powershell", "-NoProfile", "-NonInteractive", "-EncodedCommand", encoded]
                use_shell = False
            else:
                invocation, use_shell = command, True
            result = subprocess.run(invocation, shell=use_shell,
                                    cwd=project / "Assets/QuantumUser",
                                    input=json.dumps({"hook_event_name": event, "source": "startup",
                                                      "stop_hook_active": False}),
                                    capture_output=True, text=True, timeout=30)
            self.assertEqual(result.returncode, 0, result.stderr)
            response = json.loads(result.stdout)
            if event == "SessionStart":
                self.assertEqual(response["hookSpecificOutput"]["hookEventName"], event)
            else:
                self.assertTrue("systemMessage" in response or "decision" in response)
                self.assertNotIn("unavailable", response.get("systemMessage", ""))


if __name__ == "__main__":
    unittest.main()
