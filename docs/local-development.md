# Local development environment

Docker Compose runs the services needed for local development. The deployed system runs on three LXC containers instead (see `deploy/`).

## Requirements

- Docker Desktop (Windows/macOS) or Docker Engine with the Compose plugin (Linux)

## Start the database

```bash
cp .env.example .env      # PowerShell: Copy-Item .env.example .env
# edit .env and set POSTGRES_PASSWORD
docker compose up -d
docker compose ps         # db should show "healthy"
```

## Connect with psql

```bash
docker compose exec db psql -U company_brain -d company_brain
```

## Stop and reset

```bash
docker compose down       # stop, keep data
docker compose down -v    # stop and delete the database volume
```

If port 5432 is already in use on your machine, change `POSTGRES_PORT` in `.env`.
