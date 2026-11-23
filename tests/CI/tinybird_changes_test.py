"""Exercise the workflow's actual change-detection script against release histories."""

import os
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path


WORKFLOW = Path(__file__).resolve().parents[2] / ".github" / "workflows" / "ci.yml"


class TinybirdChangesTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory()
        self.addCleanup(self.scratch.cleanup)
        self.repo = Path(self.scratch.name)
        self.git("init", "-q")
        self.git("config", "user.email", "audit@example.test")
        self.git("config", "user.name", "Retention audit")
        (self.repo / "etc/tinybird").mkdir(parents=True)
        (self.repo / "etc/tinybird/events.datasource").write_text("legacy schema\n")
        self.released = self.commit()
        (self.repo / "etc/tinybird/events.datasource").write_text("retention schema\n")
        self.schema_push = self.commit()
        (self.repo / "backend.txt").write_text("later backend-only push\n")
        self.head = self.commit()

        self.bin = self.repo / "bin"
        self.bin.mkdir()
        gh = self.bin / "gh"
        gh.write_text("#!/bin/bash\n"
                      "printf '%s\\n' \"$*\" > \"$GH_CALL_LOG\"\n"
                      "if [ \"$MOCK_API_FAILURE\" = 1 ]; then exit 1; fi\n"
                      "printf '%s\\n' \"$MOCK_RELEASED_SHA\"\n")
        gh.chmod(0o755)
        block = WORKFLOW.read_text().split("      - name: Detect changes in etc/tinybird\n", 1)[1]
        self.script = textwrap.dedent(block.split("        run: |\n", 1)[1].split("\n      - name:", 1)[0])

    def git(self, *args):
        return subprocess.check_output(["git", *args], cwd=self.repo, text=True).strip()

    def commit(self):
        self.git("add", ".")
        self.git("commit", "-qm", "Fixture commit")
        return self.git("rev-parse", "HEAD")

    def detect(self, released=None, main=True, failure=False):
        output = self.repo / "output"
        output.write_text("")
        log = self.repo / "gh-call"
        environment = {
            **os.environ,
            "PATH": str(self.bin) + os.pathsep + os.environ["PATH"],
            "GITHUB_REF": "refs/heads/main" if main else "refs/heads/feature",
            "GITHUB_REPOSITORY": "aptabase/aptabase",
            "GITHUB_SHA": self.head,
            "GITHUB_OUTPUT": str(output),
            "BEFORE": self.schema_push,
            "GH_CALL_LOG": str(log),
            "MOCK_RELEASED_SHA": released or "",
            "MOCK_API_FAILURE": "1" if failure else "0",
        }
        result = subprocess.run(["bash", "-eo", "pipefail", "-c", self.script],
                                cwd=self.repo, env=environment, text=True, capture_output=True)
        if not failure:
            self.assertEqual(result.returncode, 0, result.stderr)
        if main:
            self.assertIn("branch=main&status=success&event=push&per_page=1", log.read_text())
        else:
            self.assertFalse(log.exists())
        return result.returncode, output.read_text().strip()

    def test_backend_only_push_retries_unfinished_schema_changes(self):
        # Comparing only with schema_push would miss the still-unreleased retention schema.
        self.assertEqual(self.detect(self.released)[1], "tinybird=true")

    def test_backend_only_push_after_successful_schema_release_skips_deploy(self):
        self.assertEqual(self.detect(self.schema_push)[1], "tinybird=false")

    def test_first_release_requires_schema_deploy(self):
        self.assertEqual(self.detect()[1], "tinybird=true")

    def test_api_failure_cannot_skip_schema_deploy(self):
        code, output = self.detect(failure=True)
        self.assertNotEqual(code, 0)
        self.assertEqual(output, "")

    def test_missing_release_commit_requires_schema_deploy(self):
        self.assertEqual(self.detect("a" * 40)[1], "tinybird=true")

    def test_release_outside_current_history_requires_schema_deploy(self):
        self.git("checkout", "--orphan", "other-history")
        self.git("commit", "-qm", "Unrelated release")
        unrelated = self.git("rev-parse", "HEAD")
        self.git("checkout", "-q", self.head)
        self.assertEqual(self.detect(unrelated)[1], "tinybird=true")

    def test_feature_branch_uses_push_base_without_github_api(self):
        self.assertEqual(self.detect(main=False)[1], "tinybird=false")


if __name__ == "__main__":
    unittest.main()
