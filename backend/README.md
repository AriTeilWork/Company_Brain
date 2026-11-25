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

Stop with `Ctrl+C`. Use `--reload` only during development.
