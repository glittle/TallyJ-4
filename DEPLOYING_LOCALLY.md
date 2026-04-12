# Deploying TallyJ Locally

This guide describes how to set up a local deployment of TallyJ 4 for testing or production use on a local web server. This is **not** for development - see `README.md` for development setup.

## Prerequisites

- **Operating System**: Windows Server 2019+ or Linux (Ubuntu 20.04+)
- **.NET Runtime**: .NET 10.0 or later
- **Database**: SQL Server 2019+ or SQL Server Express
- **Web Server**: IIS (Windows) or nginx/Apache (Linux)

## Database Setup

Install SQL Server Express and create a database named `TallyJ4`. Grant access to your application user.

## Application Deployment

### 1. Publish Backend

```bash
cd backend
dotnet publish Backend.csproj -c Release -o ../publish/backend
```

### 2. Build Frontend

```bash
cd frontend
npm ci
npm run build-production  # or npm run build-uat
```

### 3. Copy Frontend to Backend

The backend serves the frontend from a `wwwroot-{env}` folder.

**Windows:**
```cmd
# For production
xcopy /E /I frontend\dist publish\backend\wwwroot-prod\

# For UAT
xcopy /E /I frontend\dist publish\backend\wwwroot-uat\
```

**Linux/macOS:**
```bash
# For production
cp -r frontend/dist publish/backend/wwwroot-prod/

# For UAT
cp -r frontend/dist publish/backend/wwwroot-uat/
```

### 4. Configure Environment

Create `appsettings.Production.json` in `publish/backend/`:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=localhost\\SQLEXPRESS;Database=TallyJ4;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "your-secure-random-key-here-minimum-32-chars",
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com"
  },
  "Frontend": {
    "BaseUrl": "https://your-domain.com"
  }
}
```

### 5. Run Database Migrations

Start the published application once to apply migrations automatically:

```bash
cd publish/backend
dotnet Backend.dll
```

Wait for it to start (it will apply migrations on startup), then stop it (Ctrl+C).

### 6. Host the Application

**Windows with IIS:**
1. Copy `publish/backend/` to your IIS wwwroot folder
2. Create an application pool with .NET CLR Version: No Managed Code (.NET Core)
3. Configure the site to point to the folder

**Linux with nginx:**
1. Copy `publish/backend/` to `/var/www/tallyj4/`
2. Configure nginx as reverse proxy:

```nginx
server {
    listen 80;
    server_name your-domain.com;
    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

3. Start the app: `cd /var/www/tallyj4 && dotnet Backend.dll`

### 7. Configure Web Server

Ensure the web server:
- Forwards requests to the backend (default port 5000)
- Serves static files from `wwwroot-{env}/`
- Enables HTTPS/TLS
- Sets appropriate CORS headers

## Verification

1. Access the application at your configured URL
2. Use Swagger at `/swagger` to create an initial admin account:

   POST `/api/Auth/register`
   ```json
   {
     "email": "admin@yourdomain.com",
     "password": "SecurePassword123!",
     "firstName": "Admin",
     "lastName": "User"
   }
   ```

3. Log in and create a test election
4. Verify ballot entry and tallying work

## Security Notes

- Change default passwords immediately
- Use strong JWT keys (minimum 32 characters)
- Enable HTTPS in production
- Configure proper database permissions
- Set up regular backups

## Troubleshooting

- **Database connection fails**: Verify connection string and SQL Server status
- **Frontend not loading**: Check `wwwroot-{env}/` folder exists and has files
- **Authentication issues**: Check Swagger at `/swagger` for API testing
- **App won't start**: Ensure .NET 10 runtime is installed

For detailed configuration options, see `docs/DEPLOYMENT.md`.