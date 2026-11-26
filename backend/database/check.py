"""Run a read-only connectivity check: python -m database.check."""

from sqlalchemy import text

from database.session import get_engine, get_session_factory


def main() -> None:
    engine = get_engine()
    try:
        with get_session_factory()() as session:
            result = session.scalar(text("SELECT 1"))
            if result != 1:
                raise RuntimeError("Unexpected PostgreSQL connectivity check result.")
            print("PostgreSQL SELECT 1: 1")
    finally:
        engine.dispose()


if __name__ == "__main__":
    main()
