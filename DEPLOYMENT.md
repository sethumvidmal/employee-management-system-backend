# Deploying to an Ubuntu Server (existing MariaDB)

The API runs in a single Docker container. MariaDB is **not** containerised. The API connects to the
MariaDB server that is already installed on the host.

```
Internet ──► nginx (80/443, TLS) ──► ems_api container (127.0.0.1:8080) ──► MariaDB (127.0.0.1:3306)
```

The container uses `network_mode: host`, so `127.0.0.1` inside the container is the host itself.
This means:

- MariaDB can keep its default `bind-address = 127.0.0.1`. It never has to be exposed.
- The API listens on `127.0.0.1:8080` and is reached publicly only through nginx.

---

## 1. Prepare MariaDB

Check the server version. You need it for `DB_SERVER_VERSION`.

```bash
mariadb --version          # e.g. "... Distrib 10.11.8-MariaDB ..."  →  DB_SERVER_VERSION=10.11.8-mariadb
```

Create the database and an application user:

```bash
sudo mariadb
```

```sql
CREATE DATABASE ems_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Both host entries are needed: TCP connections from 127.0.0.1 may match either one
-- depending on MariaDB's skip-name-resolve setting.
CREATE USER 'ems_user'@'localhost' IDENTIFIED BY 'CHANGE_ME_STRONG_PASSWORD';
CREATE USER 'ems_user'@'127.0.0.1' IDENTIFIED BY 'CHANGE_ME_STRONG_PASSWORD';

GRANT ALL PRIVILEGES ON ems_db.* TO 'ems_user'@'localhost';
GRANT ALL PRIVILEGES ON ems_db.* TO 'ems_user'@'127.0.0.1';
FLUSH PRIVILEGES;
```

Avoid `;`, `'`, `"` and `$` in the password. They break the connection string or Docker Compose
variable interpolation.

The tables do **not** need to be created by hand. The API applies EF Core migrations automatically on
startup and seeds the default departments and accounts.

## 2. Install Docker

```bash
sudo apt update
sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER   # log out / in afterwards
```

## 3. Configure and start the API

```bash
git clone <repository-url> ems-backend
cd ems-backend
cp .env.example .env
nano .env
```

Set at least these values in `.env`:

| Variable | Value |
|----------|-------|
| `DB_PASSWORD` | the MariaDB password from step 1 |
| `DB_SERVER_VERSION` | e.g. `10.11.8-mariadb` (from `mariadb --version`) |
| `JWT_SECRET_KEY` | output of `openssl rand -base64 48` |
| `CORS_ORIGIN_*` | your frontend URL(s) |

Build and start the API:

```bash
docker compose up -d --build
docker compose logs -f api          # look for "Database migrations applied successfully."
curl http://127.0.0.1:8080/health   # → Healthy
```

## 4. nginx reverse proxy + HTTPS

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
sudo nano /etc/nginx/sites-available/ems-api
```

```nginx
server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        client_max_body_size 10m;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/ems-api /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d api.example.com
sudo ufw allow 'Nginx Full'
```

The API honours `X-Forwarded-For` / `X-Forwarded-Proto` from localhost, so client IPs and the
`https` scheme are seen correctly behind nginx.

## 5. Updating

```bash
cd ems-backend
git pull
docker compose up -d --build        # new migrations are applied automatically on startup
docker image prune -f
```

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Access denied for user 'ems_user'@'localhost'` | Password mismatch, or the user is missing for that host. Re-run the `CREATE USER` / `GRANT` statements. |
| `Unable to connect to any of the specified MySQL hosts` | Check that MariaDB is running (`systemctl status mariadb`) and that `DB_HOST`/`DB_PORT` are correct. |
| `JwtSettings:SecretKey must be at least 32 characters long` | Set `JWT_SECRET_KEY` in `.env`. |
| CORS errors in the browser | Add the frontend origin to `CORS_ORIGIN_0..3` and run `docker compose up -d`. |
| Port 8080 already in use | Change `API_PORT` in `.env` and in the nginx `proxy_pass`. |

> **Note:** `network_mode: host` is Linux-only. For local development on Windows/macOS, run the API with
> `dotnet run` against a local MariaDB (see README).
