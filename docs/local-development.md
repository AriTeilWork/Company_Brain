# Local development environment

Docker Compose runs the services needed for local development. The deployed system runs on three LXC containers instead (see `deploy/`).

## Requirements

- Docker Desktop (Windows/macOS) or Docker Engine with the Compose plugin (Linux)

## Start the development stack

Create `.env` and set `POSTGRES_PASSWORD` if you have not already configured the stack.

```bash
cp .env.example .env      # PowerShell: Copy-Item .env.example .env
```

```bash
docker compose up --build -d
docker compose ps         # db should show "healthy" and backend should be running
```

The backend starts once PostgreSQL is healthy. Compose sets its `DATABASE_URL`
from the `POSTGRES_*` credentials using `db:5432`. If the credentials contain
URL-reserved characters, set `COMPOSE_DATABASE_URL` in `.env` with the encoded
credentials and the same `db:5432` address.

- API: <http://127.0.0.1:8000/>
- Swagger UI: <http://127.0.0.1:8000/docs>
- Health check: <http://127.0.0.1:8000/api/v1/health> returns HTTP 200 and `{"status":"healthy"}`.

## Connect with psql

```bash
docker compose exec db psql -U company_brain -d company_brain
```

## Stop and reset

```bash
docker compose down       # stop, keep data in the pgdata volume
docker compose down -v    # stop and delete the database volume
```

Running `docker compose up --build -d` again reuses the existing PostgreSQL data.
If port 5432 is already in use on your machine, change `POSTGRES_PORT` in `.env`.
The backend in Compose continues to connect to `db:5432`.
