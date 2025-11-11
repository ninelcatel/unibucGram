# Docker 

- The project is configured to use the SQL Server container as the database when run with Docker Compose.
- EF Core migrations are applied automatically by the web container at startup (the project includes a retry loop). 

Dependencies (Linux and Windows 11)

Required
- Docker Engine (daemon) : used to run containers.
- Docker Compose v2 (preferred) :`docker compose`

Optional (only if you build/run from host)
- .NET 9 SDK : required only if you build or run the app from your host or generate EF migrations.
- dotnet-ef global tool : only needed if you run `dotnet ef` from the host.

Linux (Arch / Debian / RHEL)

- Arch Linux (pacman):
```bash
sudo pacman -S docker docker-compose
sudo systemctl enable --now docker
```

- Debian (apt):
```bash
sudo apt update
sudo apt install -y docker.io docker-compose-plugin
sudo systemctl enable --now docker
```

- RHEL (dnf/yum):
```bash
sudo dnf install -y docker docker-compose-plugin
sudo systemctl enable --now docker
```

Windows 11 (Docker Desktop)

- Recommended: install Docker Desktop for Windows (includes Docker Engine, Compose v2 plugin and GUI). On Windows 11 Docker Desktop uses WSL2 for Linux containers and this is the recommended mode when working with Linux images.
	- Download: https://www.docker.com/get-started
	- Enable WSL2 and install a distro (e.g., Ubuntu) from Microsoft Store if needed.
	- From WSL open your project and run `docker compose up --build` for the most Linux-like experience.

Verify SQL Server from host (Linux)

```
# simple query (non-interactive)
sqlcmd -S localhost -U SA -P '<SA_PASSWORD>' -Q "SELECT name FROM sys.databases;"

# interactive prompt:
sqlcmd -S localhost -U SA -P '<SA_PASSWORD>'
```

- Installing sqlcmd:
	- Debian: follow Microsoft's packaging docs to add the MS repo and install `mssql-tools` and `unixodbc`.
	- Arch Linux: there are AUR packages (search for `mssql-tools` / `mssql-tools-bin`).
	- Windows: use SSMS or the sqlcmd client shipped with SQL Server tools, or run `sqlcmd` from WSL after installing `mssql-tools` there. Could use Visual Studio though.

Quick usage

1. Start both services (recommended):
```sh
docker compose up --build
```

2. Start DB, then run web (alternative):
```sh
docker compose up -d database
docker compose up --build webapp
```

- For creating migrations: 
1. Recommended
```sh
docker compose up -d database

// waiting for the database to initialize

dotnet ef migrations add <migration_name>

docker compose up --build webapp
```
2. Alternative
```sh
dotnet ef migrations add <migration_name> --no-build

docker compose up --build
```