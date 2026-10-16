"""Behavioral tests in disposable local repositories; no network or real staging."""

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / "preflight.py"
SPEC = importlib.util.spec_from_file_location("publish_preflight", SCRIPT)
scanner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(scanner)


class CandidateTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="lsdf-publish-test-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.git("init", "--initial-branch=test-work")
        self.git("config", "user.name", "Fixture")
        self.git("config", "user.email", "fixture@example.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.git("config", "core.hooksPath", str(self.root / "no-hooks"))
        self.write("README.md", b"fixture\n")
        self.git("add", "--", "README.md")
        self.git("commit", "-m", "base")
        self.base = self.git("rev-parse", "HEAD").decode().strip()

    def git(self, *args):
        return subprocess.run(["git", "-C", str(self.root), *args], check=True,
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout

    def write(self, path, data):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    def stage(self, path, data):
        self.write(path, data)
        self.git("add", "--", path)

    def report(self):
        return scanner.inspect_repo(self.root, self.base)

    def test_safe_staged_candidate_is_read_only(self):
        self.stage("Assets/Combat.cs", b"// FP simulation\n")
        before = (self.git("status", "--porcelain=v1", "-z"), self.git("ls-files", "--stage", "-z"))
        result = self.report()
        self.assertEqual("NO_KNOWN_FINDINGS", result["status"])
        self.assertEqual(1, result["blobs_inspected"])
        after = (self.git("status", "--porcelain=v1", "-z"), self.git("ls-files", "--stage", "-z"))
        self.assertEqual(before, after)

    def test_reads_index_not_cleaned_worktree_and_redacts_token(self):
        token = "ghp_" + "Z" * 36
        self.stage("config.txt", token.encode())
        self.write("config.txt", b"clean worktree")
        result = self.report()
        self.assertEqual("GITHUB_TOKEN", result["findings"][0]["rule"])
        self.assertNotIn(token, json.dumps(result))

    def test_deleted_historical_secret_still_blocks(self):
        self.stage("old.txt", ("sk-proj-" + "Q" * 40).encode())
        self.git("commit", "-m", "first")
        self.git("rm", "--", "old.txt")
        self.git("commit", "-m", "second")
        self.assertIn("OPENAI_KEY", [f["rule"] for f in self.report()["findings"]])

    def test_secret_in_commit_message_blocks(self):
        self.git("commit", "--allow-empty", "-m", "ghp_" + "A" * 36)
        self.assertEqual("<commit-message>", self.report()["findings"][0]["path"])

    def test_excluded_folder_never_reads_blob(self):
        self.stage("Assets/Matthew Guz/secret.txt", b"not inspected")
        original = scanner.Repository.git
        calls = []

        def record(repository, *args):
            calls.append(args)
            return original(repository, *args)

        with patch.object(scanner.Repository, "git", record):
            report = self.report()
        self.assertEqual("EXCLUDED_NOT_SCANNED", report["findings"][0]["rule"])
        self.assertFalse(any(call[0] == "cat-file" for call in calls))

    def test_generated_and_credential_paths_block_without_deletion(self):
        for path in ("Library/cache.txt", "Game.csproj", ".env.production", "license.ulf"):
            self.stage(path, b"content")
        self.assertEqual(4, len(self.report()["findings"]))
        self.assertTrue((self.root / ".env.production").exists())

    def test_binary_and_lfs_are_not_silently_passed(self):
        self.stage("Assets/picture.bin", b"\x00\x01\x02")
        self.stage("Assets/large.asset", b"version https://git-lfs.github.com/spec/v1\noid sha256:123\n")
        rules = {f["rule"] for f in self.report()["findings"]}
        self.assertEqual({"BINARY_NOT_SCANNED", "LFS_OBJECT_NOT_SCANNED"}, rules)

    def test_large_blob_is_not_silently_passed(self):
        self.stage("large.txt", b"x" * (scanner.MAX_BYTES + 1))
        self.assertEqual("LARGE_FILE_NOT_SCANNED", self.report()["findings"][0]["rule"])

    def test_invalid_base_is_an_error(self):
        with self.assertRaises(scanner.GitReadError):
            scanner.inspect_repo(self.root, "missing-base")

    def test_cli_exit_codes_and_redacted_output(self):
        def invoke(base):
            return subprocess.run([sys.executable, "-B", str(SCRIPT), "--base", base],
                                  cwd=self.root, capture_output=True, check=False)

        self.assertEqual(0, invoke(self.base).returncode)
        failed = invoke("missing-base")
        self.assertEqual(2, failed.returncode)
        self.assertEqual("ERROR", json.loads(failed.stdout)["status"])
        token = "ghp_" + "B" * 36
        self.stage("credential.txt", token.encode())
        blocked = invoke(self.base)
        self.assertEqual(1, blocked.returncode)
        self.assertNotIn(token.encode(), blocked.stdout + blocked.stderr)


class TextTests(unittest.TestCase):
    def test_unquoted_yaml_credential(self):
        content = b"client_secret: " + b"private-value-123"
        self.assertEqual([("CREDENTIAL_ASSIGNMENT", 1)], scanner.scan_text(content))

    def test_cp949_and_utf16_credentials(self):
        text = '\uc124\uc815\npassword = "' + 'private-value-123' + '"\n'
        for encoding in ("cp949", "utf-16"):
            with self.subTest(encoding=encoding):
                self.assertEqual([("CREDENTIAL_ASSIGNMENT", 2)], scanner.scan_text(text.encode(encoding)))

    def test_meta_and_source_are_not_blanket_blocked(self):
        for path in ("Assets/QuantumUser/Simulation/Player.cs", "Assets/Player.prefab.meta"):
            self.assertIsNone(scanner.path_rule(path))


if __name__ == "__main__":
    unittest.main()
