# TallyJ 4 - Administrator Guide

## Table of Contents

1. [System Administration](#system-administration)
2. [User Management](#user-management)
3. [Database Management](#database-management)
4. [Security Configuration](#security-configuration)
5. [Backup and Recovery](#backup-and-recovery)
6. [Performance Monitoring](#performance-monitoring)
7. [System Configuration](#system-configuration)
8. [Troubleshooting](#troubleshooting)

---

## System Administration

### Administrator Responsibilities

As a TallyJ administrator, you are responsible for:

- Managing user accounts and permissions
- Configuring system settings
- Monitoring system health and performance
- Performing backups and recovery
- Applying security updates
- Troubleshooting technical issues
- Managing database integrity

### System Requirements

**Backend Server:**
- OS: Windows Server 2019+ or Linux (Ubuntu 20.04+)
- .NET Runtime: 10.0 or later
- Database: SQL Server 2019+ or SQL Server Express
- Memory: 4GB minimum, 8GB recommended
- Storage: 20GB minimum, SSD recommended

**Frontend Hosting:**
- Static web server (nginx, IIS, Apache)
- HTTPS/SSL certificate
- CDN (optional, for performance)

**Client Requirements:**
- Modern web browser (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+)
- JavaScript enabled
- WebSocket support for real-time features

---

## User Management

### Creating User Accounts

1. **Via Database (Initial Setup):**
   ```sql
   INSERT INTO AspNetUsers (Id, Email, UserName, EmailConfirmed, SecurityStamp)
   VALUES (NEWID(), 'user@example.com', 'user@example.com', 1, NEWID());
   ```

2. **Via API:**
   ```bash
   curl -X POST http://localhost:5016/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "user@example.com",
       "password": "SecurePassword123!",
       "firstName": "John",
       "lastName": "Doe"
     }'
   ```

### Assigning Roles

TallyJ uses ASP.NET Core Identity for role-based access control.

**Available Roles:**
- `Administrator`: Full system access
- `HeadTeller`: Manage elections, run tallies, assign tellers
- `Teller`: Enter ballots, view results
- `Voter`: View elections, submit online ballots

**Assign Role via Database:**
```sql
DECLARE @UserId UNIQUEIDENTIFIER = (SELECT Id FROM AspNetUsers WHERE Email = 'user@example.com');
DECLARE @RoleId UNIQUEIDENTIFIER = (SELECT Id FROM AspNetRoles WHERE Name = 'Teller');

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@UserId, @RoleId);
```

### Resetting User Passwords

**Via API:**
```bash
# Request password reset token
curl -X POST http://localhost:5016/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com"}'

# Reset password with token
curl -X POST http://localhost:5016/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "token": "RESET_TOKEN",
    "newPassword": "NewSecurePassword123!"
  }'
```

**Direct Database Reset (Emergency Only):**
```csharp
// Use ASP.NET Core Identity UserManager in C# code
var user = await _userManager.FindByEmailAsync("user@example.com");
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
await _userManager.ResetPasswordAsync(user, token, "NewPassword123!");
```

### Deactivating User Accounts

```sql
-- Lock out user account
UPDATE AspNetUsers
SET LockoutEnabled = 1,
    LockoutEnd = DATEADD(YEAR, 100, GETDATE())
WHERE Email = 'user@example.com';
```

### Viewing Active Users

```sql
-- Get all active users with roles
SELECT u.Email, u.UserName, r.Name AS Role, u.LastLoginDate
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.LockoutEnabled = 0 OR u.LockoutEnd < GETDATE()
ORDER BY u.Email;
```

---

## Database Management

### Connection String Configuration

**Production (appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "TallyJ4": "Server=production-server;Database=TallyJ4;User Id=tallyj_app;Password=***;Encrypt=True;TrustServerCertificate=False"
  }
}
```

**Security Best Practices:**
- Use dedicated database user (not `sa`)
- Grant minimum required permissions
- Use encrypted connections
- Store passwords in Azure Key Vault or similar

### Database Migrations

**List Migrations:**
```bash
cd backend
dotnet ef migrations list
```

**Apply Pending Migrations:**
```bash
dotnet ef database update
```

**Rollback to Specific Migration:**
```bash
dotnet ef database update PreviousMigrationName
```

**Generate Migration Script (for DBA review):**
```bash
dotnet ef migrations script --output migration.sql
```

### Database Maintenance

**Rebuild Indexes (Monthly):**
```sql
USE TallyJ4;
EXEC sp_MSforeachtable @command1="DBCC DBREINDEX('?')";
```

**Update Statistics (Weekly):**
```sql
EXEC sp_updatestats;
```

**Check Database Integrity:**
```sql
DBCC CHECKDB('TallyJ4') WITH NO_INFOMSGS, ALL_ERRORMSGS;
```

### Data Retention

**Archive Old Elections:**
```sql
-- Move elections older than 5 years to archive database
SELECT * INTO Archive_TallyJ4.dbo.Election
FROM Election
WHERE DateOfElection < DATEADD(YEAR, -5, GETDATE());

-- Delete from main database after verification
DELETE FROM Election
WHERE DateOfElection < DATEADD(YEAR, -5, GETDATE());
```

---

## Security Configuration

### JWT Configuration

**appsettings.Production.json:**
```json
{
  "Jwt": {
    "Key": "your-minimum-32-character-secret-key-here",
    "Issuer": "https://api.yourdomain.com",
    "Audience": "https://yourdomain.com",
    "ExpiryMinutes": 60
  }
}
```

**Generate Secure JWT Key:**
```bash
# PowerShell
$bytes = New-Object byte[] 32
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)

# Linux/macOS
openssl rand -base64 32
```

### CORS Configuration

**Program.cs:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("Production");
```

### HTTPS Enforcement

**Program.cs:**
```csharp
app.UseHttpsRedirection();
app.UseHsts(); // HTTP Strict Transport Security
```

**Web Server (nginx):**
```nginx
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    # ... rest of configuration
}
```

### Security Headers

**Program.cs or Middleware:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
    await next();
});
```

### Dependency Scanning

**Weekly Security Audit:**
```bash
# Backend
dotnet list package --vulnerable
dotnet list package --outdated

# Frontend
npm audit
npm outdated
```

**Apply Security Updates:**
```bash
# Backend
dotnet add package PackageName --version X.Y.Z

# Frontend
npm update
npm audit fix
```

---

## Backup and Recovery

### Database Backup Strategy

**Daily Full Backup:**
```sql
BACKUP DATABASE TallyJ4
TO DISK = 'C:\Backups\TallyJ4_Full_' + CONVERT(VARCHAR, GETDATE(), 112) + '.bak'
WITH COMPRESSION, INIT;
```

**Automated Backup Script (PowerShell):**
```powershell
# backup-database.ps1
$date = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Backups\TallyJ4_$date.bak"

Invoke-Sqlcmd -Query @"
BACKUP DATABASE TallyJ4
TO DISK = '$backupPath'
WITH COMPRESSION, INIT;
"@

# Keep only last 30 days of backups
Get-ChildItem "C:\Backups\TallyJ4_*.bak" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item
```

**Schedule with Task Scheduler:**
```powershell
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' -Argument '-File C:\Scripts\backup-database.ps1'
$trigger = New-ScheduledTaskTrigger -Daily -At 2am
Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "TallyJ Backup" -Description "Daily database backup"
```

### Application Backup

**Backup Checklist:**
- [ ] Database backup
- [ ] Application files (backend/publish/)
- [ ] Configuration files (appsettings.Production.json)
- [ ] Frontend static files (frontend/dist/)
- [ ] SSL certificates
- [ ] User-uploaded files (if any)

### Database Restoration

**Full Database Restore:**
```sql
-- Set database to single user mode
ALTER DATABASE TallyJ4 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Restore database
RESTORE DATABASE TallyJ4
FROM DISK = 'C:\Backups\TallyJ4_20260202.bak'
WITH REPLACE;

-- Set back to multi-user mode
ALTER DATABASE TallyJ4 SET MULTI_USER;
```

**Point-in-Time Restore:**
```sql
-- Requires transaction log backups
RESTORE DATABASE TallyJ4
FROM DISK = 'C:\Backups\TallyJ4_Full.bak'
WITH NORECOVERY;

RESTORE LOG TallyJ4
FROM DISK = 'C:\Backups\TallyJ4_Log.trn'
WITH RECOVERY, STOPAT = '2026-02-02 14:30:00';
```

### Disaster Recovery Plan

**Recovery Time Objective (RTO):** 4 hours  
**Recovery Point Objective (RPO):** 24 hours

**DR Steps:**
1. Identify failure type (database, server, application)
2. Provision new server (if needed)
3. Restore latest database backup
4. Deploy application files
5. Configure web server and SSL
6. Update DNS (if server changed)
7. Run smoke tests
8. Notify users

---

## Performance Monitoring

### Application Metrics

**Key Performance Indicators:**
- Request rate (requests/second)
- Response time (p50, p95, p99)
- Error rate (%)
- Database query time
- Memory usage
- CPU usage

### Logging Configuration

**Serilog (appsettings.Production.json):**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/tallyj4-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Performance Queries

**Slow Queries:**
```sql
-- Find slowest queries
SELECT TOP 10
    qs.execution_count,
    qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY avg_elapsed_time DESC;
```

**Database Size:**
```sql
SELECT 
    name AS DatabaseName,
    (size * 8.0 / 1024) AS SizeMB
FROM sys.master_files
WHERE database_id = DB_ID('TallyJ4');
```

**Active Connections:**
```sql
SELECT 
    DB_NAME(dbid) AS DatabaseName,
    COUNT(dbid) AS Connections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid;
```

---

## System Configuration

### Email Configuration

**SMTP Settings (appsettings.Production.json):**
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "Username": "noreply@yourdomain.com",
    "Password": "***",
    "FromAddress": "noreply@yourdomain.com",
    "FromName": "TallyJ"
  }
}
```

### Feature Flags

**Database Seeding:**
```json
{
  "Database": {
    "SeedOnStartup": false  // Set to false in production
  }
}
```

### Logging Levels

**Development:** Debug  
**Staging:** Information  
**Production:** Warning

---

## Troubleshooting

### Common Issues

#### Issue: High CPU Usage

**Diagnosis:**
```sql
-- Find CPU-intensive queries
SELECT TOP 10
    qs.total_worker_time / qs.execution_count AS avg_cpu_time,
    qs.execution_count,
    SUBSTRING(qt.text, 1, 100) AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY avg_cpu_time DESC;
```

**Solutions:**
- Add missing indexes
- Optimize slow queries
- Enable query result caching
- Scale up server resources

#### Issue: Memory Leaks

**Diagnosis:**
- Monitor application memory over time
- Check for unclosed database connections
- Review SignalR connection management

**Solutions:**
```csharp
// Ensure proper disposal
using (var context = new MainDbContext(options))
{
    // Database operations
}
```

#### Issue: Failed Migrations

**Diagnosis:**
```bash
dotnet ef migrations list
dotnet ef database get-context-info
```

**Solutions:**
- Check database connection
- Verify user permissions
- Review migration code for errors
- Apply migrations manually if needed

#### Issue: CORS Errors

**Diagnosis:** Check browser console for specific CORS error

**Solutions:**
- Verify `AllowedOrigins` in `appsettings.Production.json`
- Ensure frontend URL matches exactly (including protocol)
- Check that `app.UseCors()` is before `app.UseAuthorization()`

---

## Maintenance Schedule

### Daily
- [ ] Monitor error logs
- [ ] Check system health metrics
- [ ] Verify backups completed successfully

### Weekly
- [ ] Review performance metrics
- [ ] Update statistics (`sp_updatestats`)
- [ ] Check disk space
- [ ] Review security logs

### Monthly
- [ ] Rebuild indexes
- [ ] Review and archive old data
- [ ] Test disaster recovery process
- [ ] Review and update documentation

### Quarterly
- [ ] Security audit
- [ ] Dependency updates
- [ ] Performance optimization review
- [ ] User access review

---

**Last Updated:** February 2, 2026  
**Version:** 4.0.0
