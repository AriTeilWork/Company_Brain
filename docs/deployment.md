# Deployment

Company Brain runs on three Proxmox LXC containers provided by the instructor (Debian 13). Each container has one role.

```text
Client ──HTTP──▶ LXC 1 Gateway ──HTTP :8000──▶ LXC 2 Application ──SQL :5432──▶ LXC 3 Data
                 Nginx                          FastAPI (Uvicorn)                PostgreSQL 17
```

| Container | Internal IP | Role | Runs |
| --- | --- | --- | --- |
| harri-lamp-22 | 10.0.3.122 | Gateway | Nginx on port 80 (the only public web entry point) |
| harri-lamp-23 | 10.0.3.123 | Application | FastAPI backend as systemd service on 10.0.3.123:8000 |
| harri-lamp-24 | 10.0.3.124 | Data | PostgreSQL 17 on 10.0.3.124:5432 |

SSH access, public ports and passwords are in the instructor's email. They are not stored in this public repository.

The containers were created from a LAMP template. Apache and MariaDB are disabled on all three (`systemctl disable --now apache2 apache-htcacheclean mariadb`).

## LXC 1: Gateway (Nginx)

Configuration: [`deploy/nginx/company-brain.conf`](../deploy/nginx/company-brain.conf)

```bash
apt install -y nginx
cp company-brain.conf /etc/nginx/sites-available/company-brain
rm /etc/nginx/sites-enabled/default
ln -s /etc/nginx/sites-available/company-brain /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
```

| Path | Behaviour |
| --- | --- |
| `/health` | Gateway's own health check, returns `gateway ok` |
| `/api/` | Forwarded unchanged to FastAPI on 10.0.3.123:8000 |
| `/` | Placeholder text (later: web frontend) |

## LXC 2: Application (FastAPI)

Service unit: [`deploy/systemd/company-brain-backend.service`](../deploy/systemd/company-brain-backend.service)

```bash
apt install -y python3 python3-venv git postgresql-client curl
useradd --system --home-dir /opt/company-brain --shell /usr/sbin/nologin companybrain
git clone https://github.com/AriTeilWork/Company_Brain.git /opt/company-brain
python3 -m venv /opt/company-brain/.venv
/opt/company-brain/.venv/bin/pip install -r /opt/company-brain/backend/requirements.txt
chown -R companybrain:companybrain /opt/company-brain
```

Database settings are kept outside Git in `/etc/company-brain/backend.env` (owner root, `chmod 600`):

```text
DATABASE_URL=postgresql://company_brain:<password>@10.0.3.124:5432/company_brain
```

Install and start the service:

```bash
cp company-brain-backend.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now company-brain-backend
```

The backend runs as the unprivileged `companybrain` user and listens only on the internal IP.

### Deploy a new version

```bash
runuser -u companybrain -- git -C /opt/company-brain pull
runuser -u companybrain -- /opt/company-brain/.venv/bin/pip install -r /opt/company-brain/backend/requirements.txt
systemctl restart company-brain-backend
```

## LXC 3: Data (PostgreSQL)

```bash
apt install -y postgresql
```

`/etc/postgresql/17/main/postgresql.conf`:

```text
listen_addresses = 'localhost,10.0.3.124'
```

`/etc/postgresql/17/main/pg_hba.conf` (appended line, only the application container may connect):

```text
host  company_brain  company_brain  10.0.3.123/32  scram-sha-256
```

Role and database (UTF-8):

```bash
runuser -u postgres -- psql \
  -c "CREATE ROLE company_brain LOGIN PASSWORD '<password>';" \
  -c "CREATE DATABASE company_brain OWNER company_brain ENCODING 'UTF8' LC_COLLATE 'C.UTF-8' LC_CTYPE 'C.UTF-8' TEMPLATE template0;"
systemctl restart postgresql
```

## Verification

| Check | Command | Expected result |
| --- | --- | --- |
| Gateway is public | `curl http://<public-web-address>/health` | `gateway ok` |
| Gateway reaches backend | `curl -i http://<public-web-address>/api/` | JSON response from FastAPI (`404 {"detail":"Not Found"}` until API routes exist) |
| Backend runs | `systemctl status company-brain-backend` (LXC 2) | `active (running)` |
| Backend reaches database | `psql "$DATABASE_URL" -c "SELECT current_user;"` (LXC 2) | `company_brain` |
| Database is restricted | `psql "host=10.0.3.124 dbname=company_brain user=company_brain"` (LXC 1) | `no pg_hba.conf entry for host "10.0.3.122"` |
