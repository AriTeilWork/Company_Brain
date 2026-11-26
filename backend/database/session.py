"""Lazy, synchronous SQLAlchemy access using the settings from core.config."""

from collections.abc import Iterator
from functools import lru_cache

from sqlalchemy import Engine, create_engine, make_url
from sqlalchemy.orm import Session, sessionmaker

from core.config import get_settings


@lru_cache
def get_engine() -> Engine:
    """Share one connection pool per process, connecting only on first use."""
    url = make_url(get_settings().database_url.get_secret_value())
    # Plain postgresql:// otherwise selects psycopg2, which we do not install.
    url = url.set(drivername="postgresql+psycopg")
    return create_engine(url, pool_pre_ping=True)


@lru_cache
def get_session_factory() -> sessionmaker[Session]:
    """Reuse the factory, never a request's Session instance."""
    return sessionmaker(bind=get_engine(), autoflush=False, expire_on_commit=False)


def get_session() -> Iterator[Session]:
    """FastAPI dependency; callers explicitly commit successful writes."""
    with get_session_factory()() as session:
        try:
            yield session
        except BaseException:
            session.rollback()
            raise
