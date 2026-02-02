# TallyJ 4 - Deployment Guide

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Deployment Options](#deployment-options)
3. [Environment Configuration](#environment-configuration)
4. [Deployment Methods](#deployment-methods)
5. [Post-Deployment Verification](#post-deployment-verification)
6. [Smoke Testing](#smoke-testing)
7. [Rollback Procedures](#rollback-procedures)
8. [Monitoring and Maintenance](#monitoring-and-maintenance)

---

## Pre-Deployment Checklist

### Backend
- [ ] All tests passing (`dotnet test`)
- [ ] Database migrations prepared and tested
- [ ] `appsettings.Production.json` configured with production values
- [ ] JWT secret key generated (minimum 32 characters)
- [ ] Database connection string secured (not in source control)
- [ ] CORS origins configured for production domain
- [ ] Email/SMTP settings configured
- [ ] SSL certificates obtained and configured

### Frontend
- [ ] All tests passing (`npm run test`)
- [ ] Type checking passes (`npm run type-check`)
- [ ] Production build successful (`npm run build`)
- [ ] `.env.production` configured with production API URL
- [ ] Analytics and error reporting configured (if applicable)

### Infrastructure
- [ ] Production database server provisioned
- [ ] Web server configured (IIS, nginx, or Apache)
- [ ] SSL/TLS certificates installed
- [ ] Firewall rules configured
- [ ] Backup procedures in place
- [ ] Monitoring and logging configured

---

## Deployment Options

### Option 1: Traditional Deployment (IIS, nginx)
Best for organizations with existing server infrastructure.

**Pros:**
- Full control over server configuration
- No containerization overhead
- Familiar to traditional IT teams

**Cons:**
- Manual server configuration required
- More complex to scale horizontally

### Option 2: Docker Deployment
Best for modern cloud environments and scalability.

**Pros:**
- Consistent across environments
- Easy to scale with orchestration (Kubernetes, Docker Swarm)
- Simplified deployment process

**Cons:**
- Requires Docker knowledge
- Slight performance overhead

### Option 3: Cloud Platform (Azure, AWS)
Best for organizations leveraging cloud services.

**Pros:**
- Managed infrastructure
- Built-in scaling and high availability
- Integrated monitoring and backup

**Cons:**
- Vendor lock-in
- Ongoing cloud costs

---

## Environment Configuration

### Backend Configuration

Create `appsettings.Production.json` in the `backend/` directory:

```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=your-server;Database=TallyJ4;User Id=your-user;Password=***;Encrypt=True;TrustServerCertificate=False"
  },
  "Jwt": {
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com",
    "Key": "your-secure-256-bit-key",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "your-domain.com",
  "Cors": {
    "AllowedOrigins": ["https://your-domain.com"]
  }
}
```

**⚠️ Security Note:** Never commit production secrets to source control. Use:
- Environment variables
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault
- User Secrets (dotnet user-secrets)

### Frontend Configuration

Create `.env.production` in the `frontend/` directory:

```env
VITE_API_URL=https://api.your-domain.com/api
VITE_SIGNALR_HUB_URL=https://api.your-domain.com
VITE_APP_NAME=TallyJ
VITE_ENV=production
```

---

## Deployment Methods

### Method 1: Manual Deployment

#### Backend

1. **Build the application:**
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish
   ```

2. **Deploy files to server:**
   - Copy `./publish/` to server (e.g., `/var/www/tallyj4-api/`)
   
3. **Configure web server:**
   
   **IIS (Windows):**
   - Install .NET 10 Hosting Bundle
   - Create new website pointing to publish folder
   - Configure application pool (.NET CLR Version: No Managed Code)
   - Bind SSL certificate

   **nginx (Linux):**
   ```nginx
   server {
       listen 443 ssl http2;
       server_name api.your-domain.com;
       
       ssl_certificate /path/to/cert.pem;
       ssl_certificate_key /path/to/key.pem;
       
       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

4. **Run database migrations:**
   ```bash
   dotnet ef database update --connection "your-production-connection-string"
   ```

5. **Start the application:**
   ```bash
   # SystemD (Linux)
   sudo systemctl start tallyj4-api
   sudo systemctl enable tallyj4-api
   
   # PM2 (Cross-platform)
   pm2 start "dotnet TallyJ4.dll" --name tallyj4-api
   pm2 save
   ```

#### Frontend

1. **Build the application:**
   ```bash
   cd frontend
   npm ci
   npm run build
   ```

2. **Deploy static files:**
   - Copy `./dist/` to web server (e.g., `/var/www/tallyj4/`)

3. **Configure web server:**
   
   **nginx:**
   ```nginx
   server {
       listen 443 ssl http2;
       server_name your-domain.com;
       root /var/www/tallyj4;
       index index.html;
       
       ssl_certificate /path/to/cert.pem;
       ssl_certificate_key /path/to/key.pem;
       
       location / {
           try_files $uri $uri/ /index.html;
       }
       
       location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
           expires 1y;
           add_header Cache-Control "public, immutable";
       }
   }
   ```

---

### Method 2: Docker Deployment

1. **Configure environment:**
   ```bash
   cp .env.docker.example .env.docker
   # Edit .env.docker with production values
   ```

2. **Build Docker images:**
   ```bash
   docker-compose build
   ```

3. **Run containers:**
   ```bash
   docker-compose up -d
   ```

4. **Apply database migrations:**
   ```bash
   docker-compose exec backend dotnet ef database update
   ```

5. **Verify deployment:**
   ```bash
   docker-compose ps
   docker-compose logs -f
   ```

---

### Method 3: Automated CI/CD Deployment

#### Using GitHub Actions

1. **Configure secrets** in GitHub repository settings:
   - `DB_CONNECTION_STRING`
   - `JWT_SECRET_KEY`
   - `AZURE_WEBAPP_PUBLISH_PROFILE` (if using Azure)
   - `SSH_PRIVATE_KEY`, `SSH_HOST`, `SSH_USERNAME` (if using SSH)
   - `VITE_API_URL`

2. **Push to main branch:**
   ```bash
   git push origin main
   ```

3. **Monitor deployment** in GitHub Actions tab

4. **Verify deployment** using smoke tests (see below)

---

## Post-Deployment Verification

### 1. Database Connectivity

```bash
# Check database connection
dotnet ef database get-context-info --connection "your-connection-string"
```

### 2. Backend Health Check

```bash
curl https://api.your-domain.com/health
# Expected: HTTP 200 OK
```

### 3. API Endpoint Test

```bash
# Test login endpoint
curl -X POST https://api.your-domain.com/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"TestPass123!"}'
# Expected: JSON with accessToken and refreshToken
```

### 4. Frontend Accessibility

```bash
curl -I https://your-domain.com
# Expected: HTTP 200 OK
```

### 5. WebSocket/SignalR Connection

Open browser console at `https://your-domain.com` and check for:
- No SignalR connection errors
- Successful WebSocket upgrade

---

## Smoke Testing

### Automated Smoke Test Script

Create `smoke-test.sh`:

```bash
#!/bin/bash
API_URL="https://api.your-domain.com"
FRONTEND_URL="https://your-domain.com"

echo "Running smoke tests..."

# Test 1: Backend health
if curl -sf "$API_URL/health" > /dev/null; then
    echo "✓ Backend health check passed"
else
    echo "✗ Backend health check failed"
    exit 1
fi

# Test 2: Frontend accessibility
if curl -sf "$FRONTEND_URL" > /dev/null; then
    echo "✓ Frontend accessibility passed"
else
    echo "✗ Frontend accessibility failed"
    exit 1
fi

# Test 3: API authentication
response=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tallyj.test","password":"TestPass123!"}')

if echo "$response" | grep -q "accessToken"; then
    echo "✓ API authentication passed"
else
    echo "✗ API authentication failed"
    exit 1
fi

echo "All smoke tests passed!"
```

### Manual Smoke Test Checklist

#### Authentication & Authorization
- [ ] User can log in with valid credentials
- [ ] User cannot log in with invalid credentials
- [ ] JWT token refresh works correctly
- [ ] Protected endpoints require authentication
- [ ] Role-based access control works (admin, teller, voter)

#### Elections
- [ ] Can view list of elections
- [ ] Can create new election
- [ ] Can edit existing election
- [ ] Can delete election
- [ ] Can view election details

#### People Management
- [ ] Can view list of people
- [ ] Can add new person
- [ ] Can edit person details
- [ ] Can delete person
- [ ] Can search/filter people

#### Ballots & Voting
- [ ] Can create ballot
- [ ] Can record votes on ballot
- [ ] Ballot status updates correctly
- [ ] Invalid votes flagged appropriately

#### Tally & Results
- [ ] Tally algorithm runs successfully
- [ ] Results display correctly
- [ ] Vote counts accurate
- [ ] Tie scenarios handled properly

#### Real-time Features
- [ ] SignalR connection established
- [ ] Real-time updates received (ballot status, tally progress)
- [ ] Multiple users see synchronized data

#### Reports & Export
- [ ] Can generate reports
- [ ] Can export to PDF
- [ ] Can export to Excel
- [ ] Can export to CSV

#### Performance
- [ ] Pages load within 2 seconds
- [ ] Large datasets (1000+ records) render without lag
- [ ] No memory leaks during extended use

#### Security
- [ ] HTTPS enforced
- [ ] Security headers present (X-Frame-Options, CSP, etc.)
- [ ] Sensitive data not exposed in logs
- [ ] SQL injection protection verified
- [ ] XSS protection verified

---

## Rollback Procedures

### Quick Rollback (Docker)

```bash
# Stop current containers
docker-compose down

# Checkout previous version
git checkout <previous-commit-hash>

# Rebuild and restart
docker-compose build
docker-compose up -d
```

### Database Rollback

```bash
# Rollback to specific migration
dotnet ef database update <PreviousMigrationName> \
  --connection "your-connection-string"
```

### Traditional Deployment Rollback

1. Stop application service
2. Replace published files with backup
3. Rollback database migration
4. Restart application service

---

## Monitoring and Maintenance

### Application Monitoring

**Logs Location:**
- Backend: `backend/logs/tallyj4-YYYYMMDD.log`
- Docker: `docker-compose logs -f backend`

**Key Metrics to Monitor:**
- Request rate (requests/second)
- Response time (95th percentile)
- Error rate (%)
- Database connection pool usage
- Memory usage
- CPU usage

**Recommended Tools:**
- Application Insights (Azure)
- CloudWatch (AWS)
- Prometheus + Grafana
- Sentry (error tracking)
- New Relic

### Database Maintenance

```bash
# Regular backups
sqlcmd -S your-server -d TallyJ4 -Q "BACKUP DATABASE TallyJ4 TO DISK = 'C:\Backups\TallyJ4.bak'"

# Index optimization (monthly)
sqlcmd -S your-server -d TallyJ4 -Q "EXEC sp_updatestats"
```

### Security Updates

- **Weekly:** Review dependency updates
  ```bash
  dotnet list package --outdated
  npm outdated
  ```

- **Monthly:** Apply security patches
  ```bash
  dotnet add package <PackageName>
  npm update
  ```

- **Quarterly:** Security audit
  ```bash
  dotnet list package --vulnerable
  npm audit
  ```

---

## Troubleshooting

### Common Issues

#### Issue: "Database connection failed"
**Solution:**
- Verify connection string in `appsettings.Production.json`
- Check firewall allows SQL Server port (1433)
- Verify SQL Server service is running
- Test connection with `sqlcmd`

#### Issue: "CORS error in browser"
**Solution:**
- Verify `AllowedOrigins` in `appsettings.Production.json`
- Ensure frontend URL matches exactly (including protocol)
- Check browser console for specific CORS error

#### Issue: "SignalR connection failed"
**Solution:**
- Verify WebSocket support enabled in web server
- Check proxy configuration allows WebSocket upgrade
- Verify SignalR hub URL in frontend `.env.production`

#### Issue: "JWT token invalid"
**Solution:**
- Verify `Jwt:Key` is consistent across all backend instances
- Check token expiry time (`Jwt:ExpiryMinutes`)
- Ensure system clocks are synchronized

---

## Support

For deployment issues:
1. Check logs: `backend/logs/` or `docker-compose logs`
2. Verify configuration: `appsettings.Production.json` and `.env.production`
3. Run smoke tests to isolate issue
4. Contact support with logs and error details

---

**Last Updated:** February 2, 2026  
**Version:** 4.0.0
