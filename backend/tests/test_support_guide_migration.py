import os
import tempfile
import unittest
from datetime import date
from pathlib import Path
from unittest.mock import patch
from uuid import uuid4

from alembic import command
from alembic.config import Config
from sqlalchemy import create_engine, func, inspect, select, text
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session
from sqlalchemy.schema import CreateSchema, DropSchema

from database.session import get_engine
from models import SupportGuideRevision


BACKEND = Path(__file__).resolve().parents[1]
REVISION = "0044_support_guide_revisions"
MARKDOWN = "# Loan device policy\n\n- See [after-hours support](after-hours-support.md).\n"


class SupportGuideMigrationTests(unittest.TestCase):
    def verify_migration(self, engine) -> None:
        config = Config(str(BACKEND / "alembic.ini"))
        with patch("database.session.get_engine", return_value=engine):
            command.upgrade(config, REVISION)
            command.check(config)

            revisions = [
                (
                    "GUIDE-101", "1.0", "archived", date(2026, 1, 1), date(2026, 11, 15),
                    "archive/loan-device-policy-v1.md",
                ),
                (
                    "GUIDE-101", "2.0", "published", date(2026, 11, 16), None,
                    "current/loan-device-policy-v2.md",
                ),
                (
                    "GUIDE-101", "3.0", "draft", None, None,
                    "drafts/loan-device-policy-v3.md",
                ),
                (
                    "GUIDE-102", "1.0", "published", date(2026, 10, 19), None,
                    "current/laptop-will-not-start.md",
                ),
                (
                    "GUIDE-103", "1.0", "published", date(2026, 10, 1), None,
                    "current/after-hours-support.md",
                ),
                (
                    "GUIDE-104", "1.0", "published", date(2026, 10, 15), None,
                    "current/device-replacement.md",
                ),
                (
                    "GUIDE-105", "1.0", "published", date(2026, 10, 20), None,
                    "current/ticket-handover.md",
                ),
                (
                    "GUIDE-106", "1.0", "published", date(2026, 10, 20), None,
                    "current/sign-in-and-mfa.md",
                ),
            ]
            with Session(engine) as session:
                session.add_all([
                    SupportGuideRevision(
                        guide_id=guide_id,
                        version=version,
                        title="Support guide",
                        status=status,
                        effective_from=start,
                        effective_until=end,
                        markdown_body=MARKDOWN,
                        source_path=path,
                    )
                    for guide_id, version, status, start, end, path in revisions
                ])
                session.commit()

                self.assertEqual(
                    session.scalar(select(func.count()).select_from(SupportGuideRevision)), 8
                )
                self.assertEqual(
                    session.scalar(select(func.count(func.distinct(SupportGuideRevision.guide_id)))), 6
                )
                loan_revisions = session.scalars(
                    select(SupportGuideRevision)
                    .where(SupportGuideRevision.guide_id == "GUIDE-101")
                    .order_by(SupportGuideRevision.version)
                ).all()
                self.assertEqual(
                    [(guide.version, guide.status) for guide in loan_revisions],
                    [("1.0", "archived"), ("2.0", "published"), ("3.0", "draft")],
                )
                self.assertEqual(loan_revisions[1].markdown_body, MARKDOWN)
                self.assertEqual(loan_revisions[1].source_path, "current/loan-device-policy-v2.md")
                self.assertIsNone(loan_revisions[2].effective_from)
                self.assertIsNone(loan_revisions[2].effective_until)

            table = SupportGuideRevision.__table__
            valid = {
                "guide_id": "GUIDE-107",
                "version": "1.0",
                "title": "Support guide",
                "status": "published",
                "effective_from": date(2026, 11, 20),
                "effective_until": date(2026, 11, 20),
                "markdown_body": MARKDOWN,
                "source_path": "current/test-guide.md",
            }
            invalid_cases = {
                "duplicate key": {"guide_id": "GUIDE-101", "version": "2.0"},
                "unknown status": {"status": "pending"},
                "published without start": {"effective_from": None},
                "reversed dates": {"effective_until": date(2026, 11, 19)},
                "draft with reversed dates": {
                    "status": "draft", "effective_until": date(2026, 11, 19)
                },
            }
            for case, changes in invalid_cases.items():
                with self.subTest(case=case), self.assertRaises(IntegrityError):
                    with engine.begin() as connection:
                        connection.execute(table.insert().values(valid | changes))

            with engine.begin() as connection:
                connection.execute(table.insert().values(valid))
                connection.execute(text("CREATE TABLE domain_probe (id INTEGER PRIMARY KEY)"))
                connection.execute(text("INSERT INTO domain_probe VALUES (42)"))

            command.downgrade(config, "0001")
            self.assertNotIn(table.name, inspect(engine).get_table_names())
            with engine.connect() as connection:
                self.assertEqual(connection.scalar(text("SELECT id FROM domain_probe")), 42)

            command.upgrade(config, REVISION)
            with engine.connect() as connection:
                self.assertEqual(connection.scalar(select(func.count()).select_from(table)), 0)
                self.assertEqual(connection.scalar(text("SELECT id FROM domain_probe")), 42)

    def test_sqlite_migration(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            engine = create_engine(f"sqlite:///{Path(directory) / 'guides.db'}")
            try:
                self.verify_migration(engine)
            finally:
                engine.dispose()

    @unittest.skipUnless(
        os.environ.get("COMPANY_BRAIN_TEST_POSTGRES_MIGRATIONS") == "1",
        "Set COMPANY_BRAIN_TEST_POSTGRES_MIGRATIONS=1 to test migrations in an isolated PostgreSQL schema.",
    )
    def test_postgresql_migration(self) -> None:
        admin = get_engine()
        schema = f"guide_test_{uuid4().hex}"
        with admin.begin() as connection:
            connection.execute(CreateSchema(schema))

        engine = create_engine(admin.url, connect_args={"options": f"-csearch_path={schema}"})
        try:
            self.verify_migration(engine)
        finally:
            engine.dispose()
            with admin.begin() as connection:
                connection.execute(DropSchema(schema, cascade=True))
            admin.dispose()
