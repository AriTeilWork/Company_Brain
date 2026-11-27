import io
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from alembic import command
from alembic.config import Config
from alembic.migration import MigrationContext
from alembic.script import ScriptDirectory
from sqlalchemy import Column, Integer, Table, create_engine, inspect

from database.base import Base

BACKEND = Path(__file__).resolve().parents[1]


def migration_config(**kwargs) -> Config:
    return Config(str(BACKEND / "alembic.ini"), **kwargs)


class MigrationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.engine = create_engine(f"sqlite:///{Path(self.temporary.name) / 'migration.db'}")
        self.addCleanup(self.engine.dispose)
        self.engine_patch = patch("database.session.get_engine", return_value=self.engine)
        self.engine_patch.start()
        self.addCleanup(self.engine_patch.stop)

    def current_revision(self) -> str | None:
        with self.engine.connect() as connection:
            return MigrationContext.configure(connection).get_current_revision()

    def test_upgrade_downgrade_and_reapply_empty_baseline(self) -> None:
        config = migration_config()
        command.upgrade(config, "head")
        self.assertEqual(self.current_revision(), "0001")
        self.assertEqual(inspect(self.engine).get_table_names(), ["alembic_version"])
        command.check(config)

        command.downgrade(config, "base")
        self.assertIsNone(self.current_revision())
        command.upgrade(config, "head")
        self.assertEqual(self.current_revision(), "0001")

    def test_offline_sql_uses_configured_postgresql_url_without_exposing_password(self) -> None:
        engine = create_engine("postgresql+psycopg://test_user:p%40ss%25@db.invalid/test_db")
        self.addCleanup(engine.dispose)
        output = io.StringIO()
        with patch("database.session.get_engine", return_value=engine):
            command.upgrade(migration_config(output_buffer=output), "head", sql=True)
        sql = output.getvalue()
        self.assertIn("CREATE TABLE alembic_version", sql)
        self.assertIn("0001", sql)
        self.assertNotIn("p@ss%", sql)
        self.assertNotIn("p%40ss%25", sql)
        self.assertNotIn("db.invalid", sql)

    def test_autogeneration_reads_shared_metadata_and_renders_revision_template(self) -> None:
        config = migration_config()
        command.upgrade(config, "head")
        scripts = ScriptDirectory.from_config(config)
        revisions = Path(self.temporary.name) / "versions"
        revisions.mkdir()
        config.set_main_option("version_locations", str(revisions))
        baseline = Path(scripts.get_revision("head").path)
        (revisions / baseline.name).write_bytes(baseline.read_bytes())

        probe = Table("migration_probe", Base.metadata, Column("id", Integer, primary_key=True))
        self.addCleanup(Base.metadata.remove, probe)
        revision = command.revision(config, message="Add migration probe", autogenerate=True)
        source = Path(revision.path).read_text(encoding="utf-8")
        self.assertIn("migration_probe", source)
        self.assertIn("op.create_table", source)
        self.assertIn("op.drop_table", source)
        self.assertEqual(revision.down_revision, "0001")
        self.assertNotIn("migration_probe", inspect(self.engine).get_table_names())

    def test_paths_resolve_from_repository_root_without_database_settings(self) -> None:
        config = migration_config()
        self.assertEqual(Path(ScriptDirectory.from_config(config).dir), BACKEND / "migrations")
        self.assertIsNone(config.get_main_option("sqlalchemy.url"))
        with patch("database.session.get_settings", side_effect=AssertionError("Settings requested")):
            command.history(config)
