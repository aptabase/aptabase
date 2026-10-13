import contextlib
import importlib.util
import io
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


spec = importlib.util.spec_from_file_location(
    "deployment", Path(__file__).resolve().parents[2] / "scripts" / "deploy_tinybird.py"
)
deployment = importlib.util.module_from_spec(spec)
spec.loader.exec_module(deployment)


class RetentionDeploymentTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.project = Path(self.directory.name)
        (self.project / "tinybird.config.json").write_text("{}")
        (self.project / "datasources").mkdir()
        self.source = self.project / "datasources" / "events_daily_v3_mv.datasource"
        self.original = "SCHEMA >\n    period DateTime\nFORWARD_QUERY >\n    SELECT *\n"
        self.source.write_text(self.original)

    def invoke(self, results, check=False):
        args = ["deploy_tinybird.py", "--local", *(["--check"] if check else [])]
        with patch.object(deployment.Path, "cwd", return_value=self.project), \
             patch("sys.argv", args), patch.object(deployment, "run", side_effect=results) as run, \
             contextlib.redirect_stdout(io.StringIO()):
            code = deployment.main()
        return code, run.call_count

    def legacy_error(self):
        return subprocess.CompletedProcess([], 1, "Error on datasource 'datasources/events_daily_v3_mv.datasource': FORWARD_QUERY columns don't match datasource schema")

    def test_failed_migration_validation_restores_original_definitions(self):
        def second_check(*_):
            self.assertIn("period + toIntervalDay(1500)", self.source.read_text())
            return subprocess.CompletedProcess([], 1, "Unrelated validation failure")

        results = iter([self.legacy_error(), second_check])
        def run(*args):
            result = next(results)
            return result(*args) if callable(result) else result

        code, calls = self.invoke(run)
        self.assertEqual((code, calls), (1, 2))
        self.assertEqual(self.source.read_text(), self.original)

    def test_check_validates_legacy_mapping_without_deploying_or_leaving_edits(self):
        code, calls = self.invoke([self.legacy_error(), subprocess.CompletedProcess([], 0, "Valid")], check=True)
        self.assertEqual((code, calls), (0, 2))
        self.assertEqual(self.source.read_text(), self.original)

    def test_unrelated_errors_do_not_trigger_retention_mappings_or_deployment(self):
        code, calls = self.invoke([subprocess.CompletedProcess([], 1, "Authentication failed")])
        self.assertEqual((code, calls), (1, 1))
        self.assertEqual(self.source.read_text(), self.original)

    def test_job_tokens_are_redacted_from_logs(self):
        result = subprocess.CompletedProcess([], 0, "Job URL: https://example.test/jobs/1?token=test_secret&other=value\n")
        output = io.StringIO()
        with patch.object(deployment.subprocess, "run", return_value=result), contextlib.redirect_stdout(output):
            deployment.run(["tb"], self.project)
        self.assertNotIn("test_secret", output.getvalue())
        self.assertIn("token=[redacted]&other=value", output.getvalue())


if __name__ == "__main__":
    unittest.main()
