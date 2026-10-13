"""Deploy Tinybird, supplying the one-time retention mappings only to legacy schemas."""

import argparse
import re
import subprocess
from pathlib import Path


DAILY_FORWARD = """SELECT * EXCEPT expires_at,
    arrayElement(array(COLUMNS('^expires_at$'), period + toIntervalDay(1500)), 1) AS expires_at"""
FORWARD_QUERIES = {
    "events_daily_v3_mv": DAILY_FORWARD,
    "event_props_daily_v2_mv": DAILY_FORWARD,
    "monthly_usage_v1": """SELECT * EXCEPT (expires_at, retention_managed),
    arrayElement(array(COLUMNS('^expires_at$'), toDateTime(0)), 1) AS expires_at,
    arrayElement(array(COLUMNS('^retention_managed$'), toUInt8(0)), 1) AS retention_managed""",
}


def run(command, project):
    result = subprocess.run(command, cwd=project, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    # The CLI's job URLs contain credentials; keep them out of deployment logs.
    print(re.sub(r"([?&]token=)[^&\s]+", r"\1[redacted]", result.stdout), end="", flush=True)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--local", action="store_true")
    mode.add_argument("--cloud", action="store_true")
    mode.add_argument("--branch")
    parser.add_argument("--host")
    parser.add_argument("--token")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    if not (Path.cwd() / "tinybird.config.json").is_file():
        parser.error("Run this script from the Tinybird project directory (etc/tinybird)")
    command = ["tb", "--branch", args.branch] if args.branch else ["tb", "--local" if args.local else "--cloud"]
    for option in ("host", "token"):
        value = getattr(args, option)
        if value:
            command.extend(["--" + option, value])

    # Keep the CLI in its original project so its workspace/branch selection stays the same.
    # Restore temporary mappings on exit, including failed checks or Ctrl-C.
    project = Path.cwd()
    originals = {}
    try:
        check = run([*command, "deploy", "--check"], project)
        if check.returncode:
            mapped = []
            for name, query in FORWARD_QUERIES.items():
                error = f"Error on datasource 'datasources/{name}.datasource': FORWARD_QUERY columns don't match"
                if error not in check.stdout:
                    continue
                path = project / "datasources" / f"{name}.datasource"
                content = path.read_text()
                # Only adapt this feature's identity query. Other schema transformations must
                # be handled explicitly by their author rather than replaced automatically.
                if not re.search(r"FORWARD_QUERY\s*>\s*SELECT\s+\*\s*\Z", content):
                    continue
                content = re.sub(r"FORWARD_QUERY\s*>.*\Z", "FORWARD_QUERY >\n    " + query.replace("\n", "\n    ") + "\n", content, flags=re.S)
                originals[path] = path.read_text()
                path.write_text(content)
                mapped.append(name)
            if not mapped:
                return check.returncode
            print("Applying legacy retention mappings: " + ", ".join(mapped), flush=True)
            check = run([*command, "deploy", "--check"], project)
        if check.returncode or args.check:
            return check.returncode
        return run([*command, "deploy"], project).returncode
    finally:
        for path, content in originals.items():
            path.write_text(content)


if __name__ == "__main__":
    raise SystemExit(main())
