#!/usr/bin/env python3
"""Read-only, dependency-free Quantum policy checks. Not a C# compiler or replay test."""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "tools/ai/quantum-baseline.json"
SIMULATION = "Assets/QuantumUser/Simulation"
EXCLUDED = {"dinv", "gabrielaguiarproductions", "matthew guz", "generated"}
RULES = {
    "Q001-local-state": r"\bPlayerPrefs\s*\.",
    "Q002-float-conversion": r"\b(?:AsFloat|AsDouble|FromFloat_UNSAFE|FromString_UNSAFE)\b",
    "Q003-floating-math": r"\b(?:float|double|Mathf)\b|\b\d+(?:\.\d+)?[fFdD]\b",
    "Q004-unity-behaviour": r"\b(?:MonoBehaviour|GameObject|Rigidbody2D|Collider2D)\b",
    "Q005-nondeterministic-api": r"\b(?:UnityEngine\s*\.\s*)?(?:Time\s*\.\s*(?:deltaTime|time|realtimeSinceStartup)|Random\s*\.|DateTime\s*\.|Guid\s*\.\s*NewGuid)|\b(?:System\s*\.\s*)?(?:IO\s*\.|Thread\s*\.|Task\s*\.)|UnityEngine\s*\.\s*Input\b",
    # Deliberately limited to the actual state fields found during this audit.
    # A semantic review is still required for other instance/static members.
    "Q006-known-shared-state": r"^\s*(?:(?:public|private|protected|internal|static)\s+)*(?:int|bool)\s+(?:currentFrame|bufferedNextAttack|lastTimeTickFrame|isGuard)\s*(?:;|=)",
}


def read_text(path: Path) -> str:
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "cp949"):
        try:
            return raw.decode(encoding)
        except UnicodeDecodeError:
            pass
    return raw.decode("utf-8", errors="replace")


def code_only(source: str) -> str:
    """Mask ordinary C# comments/strings, preserving line numbers. Not a parser.

    Interpolated expressions and C# raw strings are not semantically analyzed.
    Rules intentionally focus on executable tokens outside ordinary strings.
    """
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\x27(?:\\.|[^\x27\\])*\x27'
    return re.sub(pattern, lambda m: re.sub(r"[^\n]", " ", m.group()), source)


def source_files(root: Path):
    directory = root / SIMULATION
    if not directory.is_dir():
        raise ValueError(f"Required simulation directory missing: {SIMULATION}")
    for current, dirs, files in os.walk(directory, followlinks=False):
        dirs[:] = sorted(d for d in dirs if d.casefold() not in EXCLUDED
                         and not (Path(current) / d).is_symlink())
        for name in sorted(files):
            path = Path(current) / name
            if path.suffix == ".cs" and not path.is_symlink():
                yield path


def analyze_text(relative: str, source: str):
    result = []
    for number, line in enumerate(code_only(source).splitlines(), 1):
        normalized = " ".join(line.split())
        for rule, pattern in RULES.items():
            if re.search(pattern, line):
                digest = hashlib.sha256(normalized.encode("utf-8")).hexdigest()[:20]
                result.append({"rule": rule, "path": relative, "line": number,
                               "fingerprint": f"{rule}|{relative}|{digest}"})
    return result


def scan(root: Path):
    result = []
    for path in source_files(root):
        result.extend(analyze_text(path.relative_to(root).as_posix(), read_text(path)))
    return result


def compare(findings, baseline):
    remaining = Counter(baseline)
    new = []
    for finding in findings:
        key = finding["fingerprint"]
        if remaining[key] > 0:
            remaining[key] -= 1
        else:
            new.append(finding)
    return new, sum(max(0, count) for count in remaining.values())


def validate_baseline(data):
    if data.get("schema") != 1 or data.get("scope") != SIMULATION:
        raise ValueError("Baseline schema/scope mismatch")
    entries = data.get("entries")
    if not isinstance(entries, dict) or any(type(v) is not int or v <= 0 for v in entries.values()):
        raise ValueError("Invalid baseline entries")
    return entries


def check(root: Path):
    findings = scan(root)
    baseline = validate_baseline(json.loads(read_text(root / BASELINE)))
    new, removed = compare(findings, baseline)
    return {"status": "fail" if new else "pass", "scope": SIMULATION,
            "files_scanned": len(list(source_files(root))),
            "existing_findings": len(findings) - len(new), "new_findings": new,
            "resolved_baseline_occurrences": removed,
            "limits": "Lexical ratchet only; not compilation, rollback, checksum or gameplay proof."}


def git_status(root: Path):
    result = subprocess.run(["git", "status", "--short", "--untracked-files=normal"],
                            cwd=root, capture_output=True, text=True, encoding="utf-8",
                            errors="replace", timeout=10)
    if result.returncode:
        raise ValueError("git status failed: " + result.stderr.strip())
    return result.stdout.strip()


def hook_response(root: Path, event: dict):
    name = event.get("hook_event_name")
    if name == "SessionStart":
        try:
            worktree = f"Working tree has existing changes: {bool(git_status(root))}. "
        except (OSError, ValueError, subprocess.SubprocessError) as exc:
            reason = str(exc).splitlines()[0][:300]
            worktree = ("Working tree status: UNKNOWN. Git status unavailable: " + reason +
                        ". Do not assume a clean tree or change Git trust settings automatically. ")
        summary = "docs/ai/current-state.md"
        context = (f"LSDF: read AGENTS.md, then {summary} before Skill.md and the relevant workflow. "
                   "Read the linked recent task records and evidence; compare them with current source and Git state. "
                   "Initial audits and old PASS results are historical, not proof for later edits. "
                   "Quantum 3: mutable match state belongs in Frame; assets are immutable. "
                   "Do not inspect DinV, GabrielAguiarProductions or Matthew Guz. "
                   + worktree + "Preserve ownership. "
                   "Run python tools/ai/quantum_guard.py check before handing off code changes. "
                   "Validate once after implementation; do not automatically rerun failed checks. "
                   f"Before handoff, update {summary} with recent work, unresolved issues, validation and next actions. "
                   "Hook.md describes limits; a static pass is not a determinism test.")
        if not (root / summary).is_file():
            context += (f" The {summary} summary is missing. Recover context from existing task records, "
                        "evidence and source; mark unknowns instead of assuming completion.")
        return {"hookSpecificOutput": {"hookEventName": name, "additionalContext": context}}
    if name != "Stop":
        raise ValueError(f"Unsupported event: {name}")
    result = check(root)
    if result["status"] == "pass":
        return {"systemMessage": f"Quantum guard: no new lexical findings; {result['existing_findings']} existing findings. Runtime validation is separate."}
    details = "; ".join(f"{f['path']}:{f['line']} {f['rule']}" for f in result["new_findings"][:6])
    reason = ("Quantum guard found new simulation policy findings: " + details +
              ". Review changes within the authorized task. Do not rewrite user-owned edits or "
              "expand the baseline to silence this. If outside scope, report the findings and stop.")
    if event.get("stop_hook_active"):
        return {"systemMessage": reason + " No further automatic continuation."}
    return {"decision": "block", "reason": reason}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("check", "audit", "baseline-candidate", "hook"))
    parser.add_argument("--root", type=Path, default=ROOT)
    args = parser.parse_args(argv)
    root = args.root.resolve()
    try:
        if args.action == "hook":
            event = json.load(sys.stdin)
            if not isinstance(event, dict):
                raise ValueError("Hook input must be an object")
            result = hook_response(root, event)
            status = 0
        elif args.action == "check":
            result = check(root)
            status = int(result["status"] == "fail")
        elif args.action == "audit":
            findings = scan(root)
            result = {"files_scanned": len(list(source_files(root))), "findings": findings,
                      "counts": dict(Counter(f["rule"] for f in findings))}
            status = 0
        else:
            result = {"schema": 1, "scope": SIMULATION,
                      "note": "2026-10-16 initial debt snapshot; not an approval of violations. Review every increase.",
                      "entries": dict(sorted(Counter(f["fingerprint"] for f in scan(root)).items()))}
            status = 0
        print(json.dumps(result, ensure_ascii=True, indent=2))
        return status
    except (OSError, ValueError, subprocess.SubprocessError) as exc:
        if args.action == "hook":
            print(json.dumps({"systemMessage": "Quantum guard unavailable; validation NOT passed: " + str(exc)}))
            return 0  # Advisory failure, never trap an agent in a broken-tool loop.
        print(json.dumps({"status": "error", "error": str(exc)}))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
