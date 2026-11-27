"""Opt-in, read-only checks against the configured development PostgreSQL."""

import os
import unittest

from sqlalchemy import text
from sqlalchemy.exc import DBAPIError

from database.session import get_engine, get_session, get_session_factory


@unittest.skipUnless(
    os.environ.get("COMPANY_BRAIN_TEST_POSTGRES") == "1",
    "Set COMPANY_BRAIN_TEST_POSTGRES=1 to check the configured development database.",
)
class PostgreSQLTests(unittest.TestCase):
    @classmethod
    def tearDownClass(cls) -> None:
        get_engine().dispose()
        get_session_factory.cache_clear()
        get_engine.cache_clear()

    def test_select_one(self) -> None:
        dependency = get_session()
        try:
            session = next(dependency)
            self.assertEqual(session.scalar(text("SELECT 1")), 1)
        finally:
            dependency.close()
        self.assertEqual(get_engine().pool.checkedout(), 0)

    def test_failed_transaction_is_cleaned_up_before_next_request(self) -> None:
        dependency = get_session()
        session = next(dependency)
        try:
            with self.assertRaises(DBAPIError) as raised:
                session.execute(text("SELECT 1 / 0"))
            with self.assertRaises(DBAPIError):
                dependency.throw(raised.exception)
        finally:
            dependency.close()
        self.assertEqual(get_engine().pool.checkedout(), 0)

        following = get_session()
        try:
            self.assertEqual(next(following).scalar(text("SELECT 1")), 1)
        finally:
            following.close()
        self.assertEqual(get_engine().pool.checkedout(), 0)
