# Backend

Python 3.13, FastAPI and Uvicorn. See the
[project architecture](https://github.com/AriTeilWork/Company_Brain/wiki/Architecture).

The backend is organized by responsibility:

| Directory | Responsibility |
| --- | --- |
| [`api/`](api/) | HTTP routes and API boundary logic. |
| [`core/`](core/) | Application configuration and shared core concerns. |
| [`database/`](database/) | Database connection and persistence setup. |
| [`models/`](models/) | Domain and persistence models. |
| [`schemas/`](schemas/) | Request, response and validation schemas. |
| [`services/`](services/) | Business logic used by API handlers. |

Keep desktop application code in [`../desktop/`](../desktop/) and project
documentation in [`../docs/`](../docs/).

## Local development

Run from `backend/`.

### Windows PowerShell

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m uvicorn main:app --reload
```

### Linux / macOS

```sh
python3.13 -m venv .venv
.venv/bin/python -m pip install -r requirements.txt
.venv/bin/python -m uvicorn main:app --reload
```

API: <http://127.0.0.1:8000/>.
Swagger UI: <http://127.0.0.1:8000/docs>.
OpenAPI: <http://127.0.0.1:8000/openapi.json>.
Health check: `GET /api/v1/health` returns `{"status": "healthy"}`.

Stop with `Ctrl+C`. Use `--reload` only during development.

## Configuration

Settings are defined in [`core/config.py`](core/config.py) and loaded from
environment variables. Locally they are also read from the repository root
`.env` (copy [`.env.example`](../.env.example)). Real environment variables take
precedence over `.env`.

| Variable | Required | Description |
| --- | --- | --- |
| `DATABASE_URL` | Yes, when the database is used | PostgreSQL URL, `postgresql://user:password@host:5432/database` |

Choose the host for where the backend runs:

| Environment | Where the value is set | Host |
| --- | --- | --- |
| Local (uvicorn on your machine) | Repository root `.env` | `localhost` |
| Docker Compose | Compose environment / `.env` | `db` |
| LXC deployment | `/etc/company-brain/backend.env` | `10.0.3.124` |

Use the settings in code with `from core.config import get_settings`.
Settings load on first use, so the API and `/docs` start without a database.
A missing or invalid `DATABASE_URL` raises a validation error that does not
include the password.

## PostgreSQL sessions

The shared synchronous engine and request-scoped session dependency are in
[`database/session.py`](database/session.py). See [database setup and checks](database/README.md)
for usage, transaction ownership, and a read-only `SELECT 1` check against the
existing development database. Installing the pinned requirements supplies
SQLAlchemy and Psycopg 3 with its bundled PostgreSQL client libraries.

## Database migrations

Alembic uses the backend's `DATABASE_URL` and `Base.metadata`.
Run from `backend/` after installing requirements and configuring the database.
On Linux/macOS, replace `.\.venv\Scripts\python.exe` with `.venv/bin/python`.

```powershell
.\.venv\Scripts\python.exe -m alembic current
.\.venv\Scripts\python.exe -m alembic history
.\.venv\Scripts\python.exe -m alembic upgrade head
.\.venv\Scripts\python.exe -m alembic revision --autogenerate -m "Add customer table"
.\.venv\Scripts\python.exe -m alembic downgrade -1
```

Models inherit `database.base.Base`; import their modules in `models/__init__.py`.
Review generated revisions before applying them; downgrades can remove data.
Omit `--autogenerate` for a handwritten revision. From the repository root,
pass `-c backend/alembic.ini`.

Revision `0001` creates only `alembic_version`, without domain tables.
Migrations are manual and do not run during API startup.
