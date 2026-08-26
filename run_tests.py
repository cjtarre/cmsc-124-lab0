#!/usr/bin/env python3
"""
cmsc-124-harness: a language-agnostic test runner for CMSC 124 laboratory activities.

This script never parses a pair's grammar. It only ever invokes the pair's own
`./run` entrypoint on committed test files, then diffs stdout + exit code against
expectations the pair themselves committed (either inline `// expect:` comments,
or sidecar .expected/.exit files). It is identical across every pair and every
host language -- see manifest.json in each test folder for the per-folder knobs.

Usage:
    python3 run_tests.py <test-folder> [--repo-root <path-to-repo-root>]

Exit code of this script itself: 0 if every test in the folder passed, 1 otherwise.
This is deliberate -- CI can gate on this script's own exit code directly.
"""

import argparse
import difflib
import json
import os
import re
import shutil
import signal
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

IS_WINDOWS = os.name == "nt"

# Extensions Windows can hand to CreateProcess directly. Anything else is a
# script that needs its interpreter named explicitly.
WINDOWS_NATIVE_SUFFIXES = {".exe", ".com", ".bat", ".cmd"}

# Where Git for Windows puts bash when its bin directory is not on PATH, which
# is the default for the "Git from the command line" install option.
WINDOWS_BASH_FALLBACKS = (
    r"C:\Program Files\Git\bin\bash.exe",
    r"C:\Program Files (x86)\Git\bin\bash.exe",
)

DEFAULT_MANIFEST = {
    # Extension of source files under this folder that count as test cases.
    "ext": ".src",
    # Optional CLI flag passed to `./run` before the test file path, e.g. "--tokenize".
    # Omit (null) for plain-execution labs.
    "flag": None,
    # "inline"  -> read `// expect:` style comments out of the source file itself.
    # "sidecar" -> read a matching .expected / .exit file next to the source file.
    "mode": "sidecar",
    # Only used when mode == "inline". The comment prefix the pair uses.
    # Matches the Crafting Interpreters convention by default.
    "expect_prefix": "expect:",
    "expect_error_prefix": "expect runtime error:",
    "expect_compile_error_prefix": "expect error:",
    # Says, on purpose, that a test file produces no output at all. Without it,
    # a file whose annotations are all typo'd would assert nothing and pass.
    "expect_nothing_prefix": "expect nothing",
    # Only used when mode == "inline": how a comment starts, and optionally
    # ends, in the pair's own invented language. Either field accepts one token
    # or a list of them. A suffix is only needed for bracketed comments, where
    # it gets stripped off the end of the annotation.
    "comment_prefix": "//",
    "comment_suffix": None,
    # Path (relative to repo root) to the pair's run entrypoint.
    "run_entrypoint": "./run",
}

# How long a single test file gets before the harness gives up on it.
TIMEOUT_SECONDS = 15

EXIT_OK = 0
EXIT_STATIC_ERROR = 65
EXIT_RUNTIME_ERROR = 70


class ConfigError(Exception):
    """A problem in a manifest, reported as an error message rather than a traceback."""


@dataclass
class TestResult:
    name: str
    passed: bool
    detail: str = ""


@dataclass
class InlineExpectations:
    """What a test file's annotation comments claim should happen."""

    stdout_lines: list = field(default_factory=list)
    # Each entry is checked against stderr on its own. Diagnostics do not always
    # come out adjacent, and one blob match cannot say which one went missing.
    stderr_substrings: list = field(default_factory=list)
    exit_code: int = EXIT_OK
    # Comments that read like an annotation but did not parse as one.
    malformed: list = field(default_factory=list)
    # True when the file explicitly claims it produces no output.
    declared_silent: bool = False

    @property
    def is_empty(self):
        return not self.stdout_lines and not self.stderr_substrings


@dataclass
class Summary:
    results: list = field(default_factory=list)

    def add(self, r: TestResult):
        self.results.append(r)

    @property
    def failed(self):
        return [r for r in self.results if not r.passed]

    def print_report(self):
        for r in self.results:
            status = "PASS" if r.passed else "FAIL"
            print(f"[{status}] {r.name}")
            if not r.passed and r.detail:
                for line in r.detail.splitlines():
                    print(f"       {line}")
        total = len(self.results)
        ok = total - len(self.failed)
        print(f"\n{ok}/{total} tests passed.")


def load_manifest(folder: Path) -> dict:
    """
    Reads manifest.json, layered over the documented defaults.

    Every problem here is a typo in a file somebody hand-wrote, so each one gets
    a sentence naming the file and the fix. An unknown key is an error rather
    than a shrug: silently falling back to the default is how a group spends an
    afternoon debugging an entrypoint that was never being read.
    """
    manifest = dict(DEFAULT_MANIFEST)
    manifest_path = folder / "manifest.json"
    if not manifest_path.exists():
        return manifest

    try:
        with open(manifest_path, encoding="utf-8") as f:
            user_manifest = json.load(f)
    except json.JSONDecodeError as exc:
        raise ConfigError(
            f"'{manifest_path}' is not valid JSON: {exc.msg} (line {exc.lineno}, column {exc.colno})."
        )
    except OSError as exc:
        raise ConfigError(f"could not read '{manifest_path}': {exc}.")

    if not isinstance(user_manifest, dict):
        raise ConfigError(
            f"'{manifest_path}' must hold a JSON object of settings, "
            f"not a {type(user_manifest).__name__}."
        )

    unknown = sorted(set(user_manifest) - set(DEFAULT_MANIFEST))
    if unknown:
        raise ConfigError(
            f"'{manifest_path}' sets {', '.join(repr(k) for k in unknown)}, which "
            "run_tests.py does not recognize. Check the spelling. The settings it "
            f"reads are: {', '.join(sorted(DEFAULT_MANIFEST))}."
        )

    manifest.update(user_manifest)
    return manifest


def find_test_files(folder: Path, ext: str):
    return sorted(folder.rglob(f"*{ext}"))


def find_windows_bash():
    """Locates a bash Windows can execute, or returns None."""
    found = shutil.which("bash")
    if found:
        return found
    for candidate in WINDOWS_BASH_FALLBACKS:
        if Path(candidate).exists():
            return candidate
    return None


def read_shebang_interpreter(script: Path):
    """
    Returns the interpreter named on a script's shebang line, e.g. "bash" for
    both `#!/bin/bash` and `#!/usr/bin/env bash`. Returns None when the file has
    no shebang or cannot be read as text.
    """
    try:
        with open(script, "rb") as f:
            first_line = f.readline(256).decode("utf-8", errors="replace")
    except OSError:
        return None

    if not first_line.startswith("#!"):
        return None

    parts = first_line[2:].strip().split()
    if not parts:
        return None

    interpreter = Path(parts[0].replace("\\", "/")).name
    if interpreter == "env" and len(parts) > 1:
        interpreter = Path(parts[1].replace("\\", "/")).name
    return interpreter


def build_launch_command(run_entrypoint: str, repo_root: Path):
    """
    Turns a pair's run entrypoint into an argv prefix the host OS can actually
    execute, and returns (argv_prefix, error_message).

    On Linux and macOS the entrypoint runs directly, exactly as the run contract
    describes. Windows cannot execute a file with a shebang line, so the
    interpreter has to be named explicitly: `run` becomes `bash run`. Without
    this, every pair working on native Windows gets WinError 193 instead of test
    results, even though their entrypoint is perfectly correct.
    """
    if not IS_WINDOWS:
        return [run_entrypoint], None

    entrypoint_path = (repo_root / run_entrypoint).resolve()
    if entrypoint_path.suffix.lower() in WINDOWS_NATIVE_SUFFIXES:
        return [run_entrypoint], None

    interpreter = read_shebang_interpreter(entrypoint_path) or "bash"

    if interpreter in ("bash", "sh", "dash", "zsh"):
        bash = find_windows_bash()
        if not bash:
            return None, (
                "ERROR: this looks like a shell script, and Windows cannot run one without bash.\n"
                "Install Git for Windows (it ships bash), or run this harness from WSL."
            )
        return [bash, run_entrypoint], None

    if interpreter.startswith("python"):
        return [sys.executable, run_entrypoint], None

    resolved = shutil.which(interpreter)
    if not resolved:
        return None, (
            f"ERROR: '{run_entrypoint}' asks for interpreter '{interpreter}', "
            "which is not on PATH on this machine."
        )
    return [resolved, run_entrypoint], None


def terminate_tree(proc):
    """
    Kills the process and everything it started.

    Killing only the process the harness launched is not enough. A run
    entrypoint is usually a shell script, so on Windows the interpreter is a
    grandchild, and it inherits the pipe the harness is about to read. Kill the
    shell alone and the interpreter keeps running with that pipe open, so the
    read never returns and grading hangs on a test that should have been
    reported as a timeout in fifteen seconds.
    """
    if IS_WINDOWS:
        # taskkill walks the tree; there is no process group to signal.
        subprocess.run(
            ["taskkill", "/F", "/T", "/PID", str(proc.pid)],
            capture_output=True,
        )
    else:
        try:
            os.killpg(os.getpgid(proc.pid), signal.SIGKILL)
        except (ProcessLookupError, PermissionError):
            pass
    proc.kill()


def run_with_timeout(cmd, repo_root: Path, timeout: int):
    """Runs a command, and makes sure it is gone when the time is up."""
    proc = subprocess.Popen(
        cmd,
        cwd=repo_root,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        # Everything in this harness is UTF-8, including whatever the
        # interpreter under test prints. Letting Python pick the platform
        # encoding instead would mangle a group's output on Windows and leave
        # it alone on the Linux runner, so the same test would pass in CI and
        # fail on their machine.
        encoding="utf-8",
        errors="replace",
        # A session of its own on POSIX, so the whole tree can be signalled.
        start_new_session=not IS_WINDOWS,
    )

    try:
        stdout, stderr = proc.communicate(timeout=timeout)
        return stdout, stderr, proc.returncode
    except subprocess.TimeoutExpired:
        terminate_tree(proc)
        # Now that nothing is left holding the pipes, this returns.
        try:
            stdout, stderr = proc.communicate(timeout=10)
        except subprocess.TimeoutExpired:
            stdout, stderr = "", ""
        return stdout, f"TIMEOUT: process exceeded {timeout}s\n{stderr}", -1


def run_program(run_entrypoint: str, flag, test_file: Path, repo_root: Path):
    cmd, error = build_launch_command(run_entrypoint, repo_root)
    if error:
        return "", error, -1

    if flag:
        cmd.append(flag)

    # Hand the entrypoint a repo-relative POSIX path. Absolute Windows paths
    # with backslashes do not survive being passed into a shell script, and
    # relative paths keep failure output identical on every platform.
    try:
        test_argument = test_file.resolve().relative_to(repo_root).as_posix()
    except ValueError:
        test_argument = str(test_file)
    cmd.append(test_argument)

    try:
        return run_with_timeout(cmd, repo_root, TIMEOUT_SECONDS)
    except FileNotFoundError:
        return "", f"ERROR: could not execute '{run_entrypoint}' -- is it committed and chmod +x?", -1
    except OSError as exc:
        return "", f"ERROR: could not execute '{run_entrypoint}' -- {exc}", -1


def as_token_list(value):
    """Normalizes a manifest field that may be a single token or a list of them."""
    if value is None:
        return []
    if isinstance(value, str):
        return [value] if value else []
    return [token for token in value if token]


def build_comment_pattern(manifest: dict):
    """
    Compiles the pattern that finds an annotation comment on a source line,
    using whatever comment syntax the group's language actually has.

    "comment_prefix" accepts one token or a list of them, so a language with
    both `#` and `--` line comments can declare both. Tokens are matched
    literally, longest first, so a `//` prefix cannot shadow a `///` one.
    """
    prefixes = as_token_list(manifest.get("comment_prefix"))
    if not prefixes:
        raise ValueError(
            "manifest.json sets no 'comment_prefix', so inline mode has no way to "
            "recognize an annotation. Give it your language's comment token, or use "
            "sidecar mode."
        )
    ordered = sorted(prefixes, key=len, reverse=True)
    alternatives = "|".join(re.escape(token) for token in ordered)
    return re.compile(f"(?:{alternatives})\\s*(.*)$")


def squash(text: str) -> str:
    """Lowercases and drops whitespace, so `Expect : 1` and `expect:1` compare equal."""
    return "".join(text.split()).lower()


def looks_like_annotation(comment: str, prefixes) -> bool:
    """
    Decides whether a comment that did not parse was nonetheless trying to be an
    annotation, so the typo can be reported instead of silently ignored.

    Two ways to look like one: the right words with the wrong spacing or case
    (`// Expect : 1`), or a misspelled keyword that still ends in a colon
    (`// expected: 1`). Prose comments have neither, so `// note: this is slow`
    stays a comment.
    """
    squashed = squash(comment)
    for prefix in prefixes:
        if squashed.startswith(squash(prefix)):
            return True

    head, separator, _ = squashed.partition(":")
    if not separator or not head:
        return False
    for prefix in prefixes:
        stem = squash(prefix).partition(":")[0][:5]
        if stem and head.startswith(stem):
            return True
    return False


def parse_inline_expectations(test_file: Path, manifest: dict) -> InlineExpectations:
    """
    Scans a source file for trailing `// expect:` style comments.

    `expect:` lines check stdout (program output). `expect runtime error:` and
    `expect error:` lines check stderr (diagnostics) and set the exit code
    accordingly -- matching the convention that runtime/static errors are
    diagnostics, not program output, and so belong on stderr per the run
    contract. `expect nothing` states that the file produces no output at all.
    Comment syntax comes from the manifest's "comment_prefix" and optional
    "comment_suffix", so a group whose invented language does not use `//`
    configures it rather than giving up on inline mode.
    """
    expect_prefix = manifest["expect_prefix"]
    error_prefix = manifest["expect_error_prefix"]
    compile_error_prefix = manifest["expect_compile_error_prefix"]
    nothing_prefix = manifest["expect_nothing_prefix"]
    known_prefixes = [
        p for p in (error_prefix, compile_error_prefix, nothing_prefix, expect_prefix) if p
    ]

    found = InlineExpectations()

    line_re = build_comment_pattern(manifest)
    suffixes = as_token_list(manifest.get("comment_suffix"))

    text = test_file.read_text(encoding="utf-8")
    for line in text.splitlines():
        m = line_re.search(line)
        if not m:
            continue
        comment = m.group(1).strip()
        for suffix in suffixes:
            if comment.endswith(suffix):
                comment = comment[: -len(suffix)].strip()
                break
        if error_prefix and comment.startswith(error_prefix):
            found.stderr_substrings.append(comment[len(error_prefix):].strip())
            found.exit_code = EXIT_RUNTIME_ERROR
        elif compile_error_prefix and comment.startswith(compile_error_prefix):
            found.stderr_substrings.append(comment[len(compile_error_prefix):].strip())
            found.exit_code = EXIT_STATIC_ERROR
        elif nothing_prefix and comment.startswith(nothing_prefix):
            found.declared_silent = True
        elif expect_prefix and comment.startswith(expect_prefix):
            found.stdout_lines.append(comment[len(expect_prefix):].strip())
        elif looks_like_annotation(comment, known_prefixes):
            found.malformed.append(comment)

    return found


def parse_sidecar_expectations(test_file: Path):
    """
    Looks for <test_file_stem>.expected and <test_file_stem>.exit next to the
    test file. .expected is compared verbatim against stdout. .exit is an
    integer exit code; if absent, 0 (success) is assumed.
    """
    expected_path = test_file.with_suffix(".expected")
    exit_path = test_file.with_suffix(".exit")

    if not expected_path.exists():
        return None, None  # signals "no sidecar found"

    expected_stdout = expected_path.read_text(encoding="utf-8")

    expected_exit = EXIT_OK
    if exit_path.exists():
        raw = exit_path.read_text(encoding="utf-8").strip()
        try:
            expected_exit = int(raw)
        except ValueError:
            raise ConfigError(
                f"'{exit_path.name}' should hold just an exit code, like 65, "
                f"but it holds {raw!r}."
            )

    return expected_stdout, expected_exit


def run_single_test(test_file: Path, manifest: dict, repo_root: Path) -> TestResult:
    name = str(test_file.relative_to(repo_root)) if test_file.is_relative_to(repo_root) else str(test_file)

    mode = manifest["mode"]
    expected_stderr_substrings = []

    if mode == "inline":
        expected = parse_inline_expectations(test_file, manifest)
        if expected.malformed:
            listed = "\n".join(f"  {comment}" for comment in expected.malformed)
            return TestResult(
                name,
                False,
                "these comments look like annotations but did not parse, so they "
                f"assert nothing:\n{listed}\n"
                f"An annotation reads exactly '{manifest['expect_prefix']} <value>'.",
            )
        if expected.is_empty and not expected.declared_silent:
            return TestResult(
                name,
                False,
                "no expectations found in this file, so it would pass no matter what "
                f"the interpreter did. Add '{manifest['expect_prefix']} <value>' "
                f"annotations, or '{manifest['expect_nothing_prefix']}' if the file "
                "really does produce no output.",
            )
        expected_stderr_substrings = expected.stderr_substrings
        expected_exit = expected.exit_code
        expected_stdout = "\n".join(expected.stdout_lines) + (
            "\n" if expected.stdout_lines else ""
        )
    elif mode == "sidecar":
        try:
            expected_stdout, expected_exit = parse_sidecar_expectations(test_file)
        except ConfigError as exc:
            return TestResult(name, False, str(exc))
        if expected_stdout is None:
            return TestResult(name, False, "No .expected sidecar file found next to this test.")
    else:
        return TestResult(name, False, f"Unknown mode '{mode}' in manifest.json.")

    stdout, stderr, actual_exit = run_program(
        manifest["run_entrypoint"], manifest.get("flag"), test_file, repo_root
    )

    problems = []

    if actual_exit != expected_exit:
        problems.append(f"exit code: expected {expected_exit}, got {actual_exit}")

    if stdout.strip("\n") != expected_stdout.strip("\n"):
        diff = "\n".join(
            difflib.unified_diff(
                expected_stdout.splitlines(),
                stdout.splitlines(),
                fromfile="expected",
                tofile="actual",
                lineterm="",
            )
        )
        problems.append(f"stdout mismatch:\n{diff}")

    # Each expected diagnostic is looked for on its own. Checking them as one
    # joined blob would demand they come out adjacent and in order, and would
    # report the whole blob as missing when only one of them was.
    missing = [wanted for wanted in expected_stderr_substrings if wanted not in stderr]
    if missing:
        wanted = "\n".join(f"  {m}" for m in missing)
        problems.append(
            f"stderr mismatch: expected to find:\n{wanted}\ngot stderr:\n  {stderr.strip()}"
        )

    if problems:
        detail = "\n".join(problems)
        if stderr.strip() and not expected_stderr_substrings:
            detail += f"\n(stderr was: {stderr.strip()})"
        return TestResult(name, False, detail)

    return TestResult(name, True)


def find_orphaned_expectations(folder: Path, test_files, repo_root: Path):
    """
    Returns .expected and .exit files that have no test file to pair with.

    Pairing is by filename stem, so renaming a test without renaming its
    expectation leaves the old expectation behind, silently unused. That is
    exactly the kind of thing nobody notices in a passing run, so it gets
    reported.
    """
    test_stems = {t.parent / t.stem for t in test_files}
    orphans = []
    for suffix in (".expected", ".exit"):
        for candidate in sorted(folder.rglob(f"*{suffix}")):
            if candidate.parent / candidate.stem not in test_stems:
                try:
                    orphans.append(str(candidate.relative_to(repo_root)))
                except ValueError:
                    orphans.append(str(candidate))
    return orphans


def main():
    parser = argparse.ArgumentParser(description="CMSC 124 language-agnostic test runner.")
    parser.add_argument("test_folder", help="Path to the folder of test files, e.g. tests/lab1")
    parser.add_argument(
        "--repo-root",
        default=".",
        help="Repo root, used to resolve the run entrypoint and relative test names. Defaults to cwd.",
    )
    args = parser.parse_args()

    repo_root = Path(args.repo_root).resolve()
    test_folder = Path(args.test_folder).resolve()

    if not test_folder.exists():
        print(f"ERROR: test folder '{test_folder}' does not exist.", file=sys.stderr)
        sys.exit(1)

    try:
        manifest = load_manifest(test_folder)
    except ConfigError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        sys.exit(1)

    test_files = find_test_files(test_folder, manifest["ext"])

    if not test_files:
        print(f"ERROR: no test files with extension '{manifest['ext']}' found under '{test_folder}'.", file=sys.stderr)
        sys.exit(1)

    if manifest["mode"] == "inline":
        try:
            build_comment_pattern(manifest)
        except ValueError as exc:
            print(f"ERROR: {exc}", file=sys.stderr)
            sys.exit(1)

    if manifest["mode"] == "sidecar":
        for orphan in find_orphaned_expectations(test_folder, test_files, repo_root):
            print(
                f"WARNING: '{orphan}' pairs with no {manifest['ext']} test file. "
                "Left over from a rename?",
                file=sys.stderr,
            )

    summary = Summary()
    for test_file in test_files:
        result = run_single_test(test_file, manifest, repo_root)
        summary.add(result)

    summary.print_report()

    sys.exit(0 if not summary.failed else 1)


if __name__ == "__main__":
    main()
