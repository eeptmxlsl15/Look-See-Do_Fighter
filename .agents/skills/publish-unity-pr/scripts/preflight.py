#!/usr/bin/env python3
"""Read-only publication candidate scan. Never stages, commits, or uploads."""

import argparse
import json
import re
import subprocess
from pathlib import Path, PurePosixPath


MAX_BYTES = 2 * 1024 * 1024
EXCLUDED = {"dinv", "gabrielaguiarproductions", "matthew guz"}
GENERATED = {"library", "temp", "obj", "logs", "usersettings", ".vs",
             "build", "builds", "__pycache__"}
SECRET_SUFFIXES = {".pem", ".p12", ".pfx", ".key", ".keystore", ".jks", ".ulf"}
PATTERNS = (
    ("PRIVATE_KEY", re.compile(r"-----BEGIN (?:[A-Z0-9 ]+ )?PRIVATE KEY-----")),
    ("GITHUB_TOKEN", re.compile(r"\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b")),
    ("OPENAI_KEY", re.compile(r"\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b")),
    ("AWS_ACCESS_ID", re.compile(r"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b")),
    ("SLACK_TOKEN", re.compile(r"\bxox[baprs]-[A-Za-z0-9-]{20,}\b")),
    ("CREDENTIAL_URL", re.compile(r"https?://[^\s/@:]+:[^\s/@]+@", re.I)),
    ("CREDENTIAL_ASSIGNMENT", re.compile(
        r'''(?i)["']?\b(?:api[_-]?key|access[_-]?token|auth[_-]?token|client[_-]?secret|password|passwd|secret[_-]?key|aws_secret_access_key)\b["']?[ \t]*[:=][ \t]*(?:["']([^"'\r\n]{8,})["']|([A-Za-z0-9_+/=.-]{12,}))''')),
)


def path_rule(path):
    parts = PurePosixPath(path.replace("\\", "/")).parts
    lowered = [part.casefold() for part in parts]
    name = lowered[-1]
    if any(part in EXCLUDED for part in lowered):
        return "EXCLUDED_NOT_SCANNED"
    if any(part in GENERATED for part in lowered):
        return "GENERATED_FILE"
    if ".claude" in lowered and "worktrees" in lowered:
        return "NESTED_WORKTREE"
    if (name == ".env" or name.startswith(".env.") or
            name in {"id_rsa", "id_ed25519", ".npmrc", ".pypirc", "credentials", "hosts.yml"} or
            PurePosixPath(name).suffix in SECRET_SUFFIXES):
        return "CREDENTIAL_FILE"
    if len(parts) == 1 and PurePosixPath(name).suffix in {".csproj", ".sln", ".slnx", ".user", ".suo"}:
        return "IDE_GENERATED_FILE"
    if PurePosixPath(name).suffix in {".log", ".tmp", ".bak", ".pyc"}:
        return "TEMPORARY_FILE"
    return None


def scan_text(data):
    if data.startswith((b"\xff\xfe", b"\xfe\xff")):
        encodings = ("utf-16",)
    elif b"\x00" in data:
        return [("BINARY_NOT_SCANNED", None)]
    else:
        encodings = ("utf-8-sig", "cp949")
    for encoding in encodings:
        try:
            content = data.decode(encoding)
            break
        except UnicodeError:
            continue
    else:
        return [("ENCODING_NOT_SCANNED", None)]
    if content.startswith("version https://git-lfs.github.com/spec/v1"):
        return [("LFS_OBJECT_NOT_SCANNED", None)]
    findings = []
    for rule, pattern in PATTERNS:
        for match in pattern.finditer(content):
            findings.append((rule, content.count("\n", 0, match.start()) + 1))
    return findings


class GitReadError(RuntimeError):
    pass


class Repository:
    def __init__(self, root):
        self.root = Path(root).resolve()

    def git(self, *args):
        result = subprocess.run(
            ["git", "-c", "safe.directory=" + self.root.as_posix(), "-C", str(self.root), *args],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False, timeout=60,
        )
        if result.returncode:
            # Raw Git stderr can contain credential URLs or object contents.
            raise GitReadError("Git read command failed: " + args[0] + " (exit " + str(result.returncode) + ")")
        return result.stdout

    def resolve(self, ref):
        return self.git("rev-parse", "--verify", "--end-of-options", ref + "^{commit}").decode().strip()


def inspect_repo(root, base):
    repo = Repository(root)
    head = repo.resolve("HEAD")
    base_sha = repo.resolve(base)
    findings = []
    inspected = set()
    blob_count = 0

    def add(source, path, rule, line=None):
        finding = {"source": source, "path": path, "rule": rule}
        if line is not None:
            finding["line"] = line
        findings.append(finding)

    def inspect(source, path, mode, oid):
        nonlocal blob_count
        rule = path_rule(path)
        if rule:
            add(source, path, rule)
            return  # In particular, never read excluded vendor contents.
        if mode not in {"100644", "100755"}:
            add(source, path, "NON_REGULAR_NOT_SCANNED")
            return
        if (path, oid) in inspected:
            return
        inspected.add((path, oid))
        if int(repo.git("cat-file", "-s", oid)) > MAX_BYTES:
            add(source, path, "LARGE_FILE_NOT_SCANNED")
            return
        blob_count += 1
        for rule, line in scan_text(repo.git("cat-file", "blob", oid)):
            add(source, path, rule, line)

    if repo.git("ls-files", "--unmerged", "-z"):
        raise GitReadError("Unmerged index; candidate cannot be inspected")
    staged_paths = set(repo.git("diff", "--cached", "--name-only", "--no-renames",
                                "--diff-filter=ACMRTUXB", "-z").split(b"\0")) - {b""}
    for record in repo.git("ls-files", "--stage", "-z").split(b"\0"):
        if not record:
            continue
        metadata, raw_path = record.split(b"\t", 1)
        if raw_path in staged_paths:
            mode, oid, stage = metadata.decode("ascii").split()
            inspect("INDEX", raw_path.decode("utf-8"), mode, oid)

    commits = repo.git("rev-list", base_sha + ".." + head).decode("ascii").splitlines()
    for commit in commits:
        for rule, line in scan_text(repo.git("show", "-s", "--format=%B", commit)):
            add(commit, "<commit-message>", rule, line)
        changed = set(repo.git("diff-tree", "--root", "--no-commit-id", "--name-only", "-r", "-m",
                               "--no-renames", "--diff-filter=ACMRTUXB", "-z", commit).split(b"\0")) - {b""}
        if not changed:
            continue
        # Tree metadata only; blobs outside the changed paths are never read.
        for record in repo.git("ls-tree", "-r", "-z", commit).split(b"\0"):
            if not record:
                continue
            metadata, raw_path = record.split(b"\t", 1)
            if raw_path in changed:
                mode, kind, oid = metadata.decode("ascii").split()
                inspect(commit, raw_path.decode("utf-8"), mode, oid)

    return {"status": "BLOCKED" if findings else "NO_KNOWN_FINDINGS",
            "head": head, "base": base_sha, "commits_inspected": len(commits),
            "staged_files": len(staged_paths), "blobs_inspected": blob_count,
            "findings": findings,
            "limitation": "Heuristic candidate scan only; not test success or publication approval."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", required=True, help="Reviewed local PR base ref, refreshed before inspection")
    args = parser.parse_args()
    try:
        report = inspect_repo(Path.cwd(), args.base)
    except (GitReadError, OSError, UnicodeError, ValueError, subprocess.TimeoutExpired) as error:
        # Do not expose raw subprocess arguments, environment or secret values.
        message = str(error) if isinstance(error, GitReadError) else type(error).__name__
        print(json.dumps({"status": "ERROR", "error": message}, ensure_ascii=True))
        return 2
    print(json.dumps(report, ensure_ascii=True, indent=2))
    return 1 if report["findings"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
