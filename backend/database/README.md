# Database

`session.py` provides the shared SQLAlchemy engine, session factory, and
synchronous FastAPI dependency. It reads `DATABASE_URL` through the settings
from #32, accepting the existing `postgresql://` format and selecting the
`postgresql+psycopg` dialect explicitly. Credentials and connection options
remain in the configured URL. Reserved characters in passwords must be URL-encoded.

The engine and factory are created lazily, once per backend process. Importing
the application does not require database settings or open a connection.
Connections are pooled and checked before reuse (`pool_pre_ping=True`).
No domain tables are created on startup or by the connectivity check.

## Request sessions

Use `session: Annotated[Session, Depends(get_session)]` in a synchronous `def`
route, importing `Annotated` from `typing`, `Depends` from `fastapi`, `Session`
from `sqlalchemy.orm`, and `get_session` from `database.session`.
Synchronous routes run in FastAPI's thread pool; avoid blocking database calls
inside an `async def` route.

Each request receives its own session. The dependency rolls back on an
exception and always closes the session, returning its connection to the pool.
Closing also rolls back any uncommitted transaction after a successful request.
Services explicitly call `session.commit()` when a write is complete; the
dependency never commits automatically. If a service catches a database error
and continues using the same session, it must call `session.rollback()` first.
Never share a session between requests or background tasks.

## Check the existing development database

Start the existing database using [local development instructions](../../docs/local-development.md)
or use an already running development PostgreSQL instance. Set `DATABASE_URL`
in the repository root `.env`, matching its actual database, user, password,
host and port (see [backend configuration](../README.md#configuration)).
Do not use the example password for a real deployment or commit `.env`.

Run from `backend/`, after installing `requirements.txt`:

```powershell
.\.venv\Scripts\python.exe -m database.check
```

```sh
.venv/bin/python -m database.check
```

Expected output: `PostgreSQL SELECT 1: 1`. A configuration or connection failure
exits with a nonzero status. The check uses the shared session factory, only
executes `SELECT 1`, closes the session and disposes its pool when finished.

## Tests

From `backend/`, run `.venv\Scripts\python.exe -m unittest discover -s tests -v`
(or `.venv/bin/python -m unittest discover -s tests -v` on Linux/macOS).
Unit tests use an isolated in-memory SQLite database and require no credentials.
PostgreSQL integration checks are opt-in: set `COMPANY_BRAIN_TEST_POSTGRES=1`
to test `SELECT 1` and recovery after a failed transaction against the configured
development database. They execute no DDL or persistent writes.
