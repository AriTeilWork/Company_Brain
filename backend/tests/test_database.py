"""Session lifecycle checks without an external database or test framework."""

import unittest
from types import SimpleNamespace
from unittest.mock import patch

from pydantic import SecretStr
from sqlalchemy import create_engine, text
from sqlalchemy.orm import Session, sessionmaker

from database.session import get_engine, get_session, get_session_factory


class ApplicationStartupTests(unittest.IsolatedAsyncioTestCase):
    async def test_api_and_docs_work_without_database_settings(self) -> None:
        from main import app

        async def receive():
            return {"type": "http.request", "body": b"", "more_body": False}

        with patch("database.session.get_settings", side_effect=AssertionError("Database settings requested")):
            for path in ("/", "/api/v1/health", "/docs", "/openapi.json"):
                with self.subTest(path=path):
                    messages = []

                    async def send(message):
                        messages.append(message)

                    scope = {
                        "type": "http", "asgi": {"version": "3.0"},
                        "http_version": "1.1", "method": "GET", "scheme": "http",
                        "path": path, "raw_path": path.encode(), "query_string": b"",
                        "root_path": "", "headers": [],
                        "server": ("testserver", 80), "client": ("testclient", 1234),
                    }
                    await app(scope, receive, send)
                    self.assertEqual(messages[0]["status"], 200)


class EngineTests(unittest.TestCase):
    def tearDown(self) -> None:
        get_session_factory.cache_clear()
        get_engine.cache_clear()

    def test_configured_url_uses_psycopg_without_connecting(self) -> None:
        settings = SimpleNamespace(database_url=SecretStr(
            "postgresql://test_user:p%40ss@db.invalid:5433/test_db?sslmode=require"
        ))
        with patch("database.session.get_settings", return_value=settings):
            engine = get_engine()
            try:
                self.assertIs(get_engine(), engine)
                self.assertEqual(engine.dialect.driver, "psycopg")
                self.assertEqual(engine.url.host, "db.invalid")
                self.assertEqual(engine.url.port, 5433)
                self.assertEqual(engine.url.database, "test_db")
                self.assertEqual(engine.url.password, "p@ss")
                self.assertEqual(engine.url.query["sslmode"], "require")
            finally:
                engine.dispose()


class TrackingSession(Session):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.cleanup = []

    def rollback(self) -> None:
        self.cleanup.append("rollback")
        super().rollback()

    def close(self) -> None:
        self.cleanup.append("close")
        super().close()


class SessionLifecycleTests(unittest.TestCase):
    def setUp(self) -> None:
        self.engine = create_engine("sqlite://")
        self.addCleanup(self.engine.dispose)
        with self.engine.begin() as connection:
            connection.execute(text("CREATE TABLE session_probe (value INTEGER)"))
        factory = sessionmaker(bind=self.engine, class_=TrackingSession)
        self.factory_patch = patch("database.session.get_session_factory", return_value=factory)
        self.factory_patch.start()
        self.addCleanup(self.factory_patch.stop)

    def count_rows(self) -> int:
        with self.engine.connect() as connection:
            return connection.scalar(text("SELECT count(*) FROM session_probe"))

    def test_success_closes_session_without_committing_writes(self) -> None:
        dependency = get_session()
        session = next(dependency)
        session.execute(text("INSERT INTO session_probe VALUES (1)"))
        with self.assertRaises(StopIteration):
            next(dependency)
        self.assertEqual(session.cleanup, ["close"])
        self.assertEqual(self.count_rows(), 0)

    def test_failure_rolls_back_before_closing_and_propagates(self) -> None:
        dependency = get_session()
        session = next(dependency)
        session.execute(text("INSERT INTO session_probe VALUES (1)"))
        failure = RuntimeError("request failed")
        with self.assertRaises(RuntimeError) as raised:
            dependency.throw(failure)
        self.assertIs(raised.exception, failure)
        self.assertEqual(session.cleanup, ["rollback", "close"])
        self.assertEqual(self.count_rows(), 0)

    def test_service_can_commit_successful_writes(self) -> None:
        dependency = get_session()
        session = next(dependency)
        session.execute(text("INSERT INTO session_probe VALUES (1)"))
        session.commit()
        list(dependency)
        self.assertEqual(self.count_rows(), 1)
        self.assertEqual(session.cleanup, ["close"])

    def test_requests_receive_separate_sessions(self) -> None:
        first, second = get_session(), get_session()
        try:
            self.assertIsNot(next(first), next(second))
        finally:
            first.close()
            second.close()

    def test_session_is_closed_even_when_rollback_fails(self) -> None:
        dependency = get_session()
        session = next(dependency)
        with patch.object(session, "rollback", side_effect=RuntimeError("rollback failed")):
            with self.assertRaisesRegex(RuntimeError, "rollback failed"):
                dependency.throw(ValueError("request failed"))
        self.assertEqual(session.cleanup, ["close"])
