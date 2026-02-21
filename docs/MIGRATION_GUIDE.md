# TallyJ v3 to v4 Migration Guide

## Table of Contents

1. [Overview](#overview)
2. [What's New in v4](#whats-new-in-v4)
3. [Breaking Changes](#breaking-changes)
4. [Data Migration](#data-migration)
5. [Feature Mapping](#feature-mapping)
6. [Migration Steps](#migration-steps)
7. [Post-Migration Verification](#post-migration-verification)
8. [Rollback Plan](#rollback-plan)

---

## Overview

TallyJ v4 is a complete rewrite of the election management system with modern technology and improved architecture. This guide will help you migrate from TallyJ v3 to v4.

### Technology Changes

| Component     | v3                              | v4                             |
| ------------- | ------------------------------- | ------------------------------ |
| **Backend**   | ASP.NET MVC (.NET Framework)    | ASP.NET Core Web API (.NET 10) |
| **Frontend**  | jQuery, Knockout.js             | Vue 3 + TypeScript             |
| **Database**  | SQL Server (Entity Framework 6) | SQL Server (EF Core 10)        |
| **Auth**      | Forms Authentication            | JWT Bearer Tokens              |
| **Real-time** | SignalR (Classic)               | SignalR Core                   |

### Migration Timeline

- **Small organizations** (1-5 elections): 1-2 days
- **Medium organizations** (5-20 elections): 3-5 days
- **Large organizations** (20+ elections): 1-2 weeks

---

## What's New in v4

### ✨ New Features

1. **Modern UI/UX**
   - Responsive design (mobile, tablet, desktop)
   - Professional design system with Element Plus
   - Dark mode support (planned)
   - Improved accessibility (WCAG 2.1 AA compliant)

2. **Enhanced Performance**
   - Faster page loads with code splitting
   - Optimized database queries
   - Real-time updates with SignalR Core
   - Progressive Web App (PWA) support

3. **Better Security**
   - JWT-based authentication
   - Refresh token rotation
   - HTTPS enforcement
   - Content Security Policy (CSP)
   - SQL injection protection

4. **Improved Developer Experience**
   - RESTful API with Swagger documentation
   - TypeScript for type safety
   - Automated testing (backend and frontend)
   - CI/CD with GitHub Actions

5. **New Capabilities**
   - Comprehensive audit logging
   - Advanced reporting and analytics
   - Data export (PDF, Excel, CSV)
   - Docker deployment support

### 🔄 Improved Features

- **Election Management**: More configuration options exposed
- **Ballot Entry**: Faster data entry with autocomplete
- **Results Display**: Better visualizations with charts
- **Import/Export**: Enhanced CSV import with validation

---

## Breaking Changes

### ⚠️ Important Differences

#### 1. Authentication

**v3:** Forms-based authentication with cookies  
**v4:** JWT bearer tokens

**Impact:** Users must log in again after migration.

**Action Required:**

- Inform users they will need to reset passwords
- Use the password reset feature in v4
- Update any API integrations to use JWT tokens

#### 2. API Endpoints

**v3:** MVC controller actions (`/Election/Create`)  
**v4:** RESTful API (`/api/elections`)

**Impact:** Any custom integrations or scripts must be updated.

**Action Required:**

- Review API documentation at `/swagger`
- Update integration scripts
- Test all API calls before production migration

#### 3. Database Schema Changes

**v3:** Entity Framework 6 conventions  
**v4:** EF Core 10 with explicit migrations

**Major Changes:**

- New primary key strategy (GUIDs instead of integers)
- Computed columns for full names and ballot codes
- Additional indexes for performance
- New tables: `Computer`, `RefreshToken`, `TwoFactorToken`

#### 4. Removed Features (Temporary)

The following v3 features are not yet implemented in v4:

- **Password reset flow** (planned for post-launch)
- **Email/SMS notifications** (backend ready, UI pending)
- **Front desk voter check-in** (entity exists, UI pending)
- **Public kiosk display** (partial implementation)

**Action Required:**

- Plan workarounds for missing features
- Contact support for timeline on missing features
- Consider staying on v3 if these are critical

---

## Data Migration

### Option 1: Fresh Start (Recommended for Small Organizations)

**Best for:**

- Organizations with 1-5 elections
- No historical data to preserve
- Willing to re-enter data

**Steps:**

1. Export active voters/candidates from v3 to CSV
2. Install TallyJ v4
3. Create new elections
4. Import people from CSV
5. Start using v4

**Pros:** Clean, simple, no migration issues  
**Cons:** Lose historical data

---

### Option 2: Database Migration Script

**Best for:**

- Organizations with significant historical data
- Need to preserve all past elections
- Have technical resources to run scripts

**Migration Script:**

```sql
-- TallyJ v3 to v4 Data Migration Script
-- WARNING: Test on a backup database first!

-- Step 1: Backup v3 database
BACKUP DATABASE TallyJ3
TO DISK = 'C:\Backups\TallyJ3_PreMigration.bak';

-- Step 2: Create v4 database
CREATE DATABASE TallyJ4;
GO

USE TallyJ4;
GO

-- Step 3: Run v4 migrations
-- (Use: dotnet ef database update)

-- Step 4: Migrate Elections
INSERT INTO TallyJ4.dbo.Election (
    ElectionGuid, Name, DateOfElection, ElectionType,
    NumberToElect, NumberExtra, TallyStatus,
    OwnerGuid, CreatedDate, LastModifiedDate
)
SELECT
    NEWID() AS ElectionGuid,
    Name,
    CAST(DateOfElection AS DATETIME2),
    CASE ElectionType
        WHEN 0 THEN 'LSA'
        WHEN 1 THEN 'NSA'
        WHEN 2 THEN 'Convention'
        ELSE 'Other'
    END AS ElectionType,
    NumberToElect,
    ISNULL(NumberExtra, 0),
    CASE Status
        WHEN 0 THEN 'Setup'
        WHEN 1 THEN 'InProgress'
        WHEN 2 THEN 'Finalized'
        ELSE 'Setup'
    END AS TallyStatus,
    NULL AS OwnerGuid,  -- Will need to be set manually
    CreatedDate,
    GETDATE()
FROM TallyJ3.dbo.Elections;

-- Create mapping table for old IDs to new GUIDs
CREATE TABLE #ElectionMapping (
    OldId INT,
    NewGuid UNIQUEIDENTIFIER
);

INSERT INTO #ElectionMapping (OldId, NewGuid)
SELECT e3.Id, e4.ElectionGuid
FROM TallyJ3.dbo.Elections e3
JOIN TallyJ4.dbo.Election e4 ON e3.Name = e4.Name;

-- Step 5: Migrate People
INSERT INTO TallyJ4.dbo.Person (
    PersonGuid, ElectionGuid, FirstName, LastName,
    CanVote, CanReceiveVotes, AgeGroup,
    CreatedDate, LastModifiedDate
)
SELECT
    NEWID(),
    em.NewGuid,
    p.FirstName,
    p.LastName,
    CAST(p.CanVote AS BIT),
    CAST(p.CanReceiveVotes AS BIT),
    ISNULL(p.AgeGroup, 'A'),
    p.CreatedDate,
    GETDATE()
FROM TallyJ3.dbo.People p
JOIN #ElectionMapping em ON p.ElectionId = em.OldId;

-- Step 6: Migrate Ballots
-- Similar pattern as above...

-- Step 7: Cleanup
DROP TABLE #ElectionMapping;

-- Step 8: Verify counts
SELECT 'Elections' AS Entity, COUNT(*) AS v3Count FROM TallyJ3.dbo.Elections
UNION ALL
SELECT 'Elections', COUNT(*) FROM TallyJ4.dbo.Election;

SELECT 'People' AS Entity, COUNT(*) FROM TallyJ3.dbo.People
UNION ALL
SELECT 'People', COUNT(*) FROM TallyJ4.dbo.Person;
```

**⚠️ Important Notes:**

- Test on a backup database first
- Review and adjust GUID mapping as needed
- User accounts must be recreated (different auth system)
- Validate data integrity after migration

---

### Option 3: Parallel Operation

**Best for:**

- Large organizations requiring zero downtime
- Gradual transition preferred
- Critical elections in progress

**Steps:**

1. Keep v3 running for current elections
2. Install v4 on separate server
3. Use v4 for new elections only
4. Gradually migrate completed elections
5. Decommission v3 when all elections moved

**Pros:** No downtime, safe transition  
**Cons:** Maintain two systems temporarily

---

## Feature Mapping

### How to Accomplish v3 Tasks in v4

#### Creating an Election

**v3:** Election → New Election  
**v4:** Elections → Create New Election

**Changes:**

- More fields available in v4
- Tally method selected upfront
- Passcode field added

#### Adding Voters

**v3:** People → Add Person  
**v4:** People → Add New Person

**Changes:**

- Same basic fields
- Location assignment in UI (if locations created first)
- Age group field added

#### Entering Ballots

**v3:** Ballots → New Ballot  
**v4:** Ballots → New Ballot

**Changes:**

- Autocomplete for name search (faster)
- Real-time validation
- Computer code must be registered first

#### Running Tally

**v3:** Tally → Calculate Results  
**v4:** Tally → Start Tally

**Changes:**

- Real-time progress bar
- Automatic re-calculation if ballots change
- Better error messages

#### Viewing Results

**v3:** Results → View  
**v4:** Results → Select Election

**Changes:**

- Enhanced visualizations
- Export options (PDF, Excel, CSV)
- Result history tracking

---

## Migration Steps

### Phase 1: Preparation (1-2 Days)

1. **Backup v3 Database**

   ```bash
   sqlcmd -S server -Q "BACKUP DATABASE TallyJ3 TO DISK='C:\Backups\TallyJ3.bak'"
   ```

2. **Export Current Data**
   - Elections list (CSV)
   - Voters/candidates (CSV)
   - Completed election results (PDF)

3. **Document Custom Configurations**
   - User accounts and roles
   - Election types used
   - Custom reports or integrations

4. **Install v4 Test Environment**
   - Follow [DEPLOYMENT.md](DEPLOYMENT.md)
   - Use staging server
   - Test with sample data

### Phase 2: Migration (1-3 Days)

1. **Choose Migration Strategy**
   - Fresh start, database script, or parallel operation

2. **Run Migration**
   - Execute chosen migration method
   - Verify data integrity

3. **Recreate User Accounts**

   ```bash
   # Use API to create users
   curl -X POST http://localhost:5016/auth/register \
     -H "Content-Type: application/json" \
     -d '{"email":"user@example.com","password":"TempPass123!"}'
   ```

4. **Assign Roles**
   - Administrator
   - HeadTeller
   - Teller

5. **Configure System**
   - Update appsettings.Production.json
   - Set up locations (if applicable)
   - Register computers

### Phase 3: Validation (1 Day)

1. **Verify Data**
   - [ ] All elections present
   - [ ] Voter counts match
   - [ ] Ballot counts match
   - [ ] Results match (for completed elections)

2. **Test Workflows**
   - [ ] Create election
   - [ ] Add voters
   - [ ] Enter ballots
   - [ ] Run tally
   - [ ] View results
   - [ ] Export reports

3. **Performance Testing**
   - [ ] Page load times acceptable
   - [ ] Large datasets (1000+ records) perform well
   - [ ] Real-time updates working

### Phase 4: Deployment (1 Day)

1. **Schedule Downtime**
   - Notify all users
   - Choose low-activity period
   - Plan 2-4 hour window

2. **Deploy Production**
   - Follow [DEPLOYMENT.md](DEPLOYMENT.md)
   - Run smoke tests
   - Verify SSL certificate

3. **User Training**
   - Brief orientation for administrators
   - Quick-start guide for tellers
   - Support contacts

4. **Go Live**
   - Open to users
   - Monitor for issues
   - Provide support

---

## Post-Migration Verification

### Data Integrity Checklist

```sql
-- Compare record counts
SELECT 'v3 Elections' AS Source, COUNT(*) AS Count FROM TallyJ3.dbo.Elections
UNION ALL
SELECT 'v4 Elections', COUNT(*) FROM TallyJ4.dbo.Election;

SELECT 'v3 People', COUNT(*) FROM TallyJ3.dbo.People
UNION ALL
SELECT 'v4 People', COUNT(*) FROM TallyJ4.dbo.Person;

SELECT 'v3 Ballots', COUNT(*) FROM TallyJ3.dbo.Ballots
UNION ALL
SELECT 'v4 Ballots', COUNT(*) FROM TallyJ4.dbo.Ballot;

SELECT 'v3 Votes', COUNT(*) FROM TallyJ3.dbo.Votes
UNION ALL
SELECT 'v4 Votes', COUNT(*) FROM TallyJ4.dbo.Vote;
```

### Functional Testing

- [ ] User login works
- [ ] Elections display correctly
- [ ] Can create new election
- [ ] Can add voters
- [ ] Can enter ballots
- [ ] Tally runs successfully
- [ ] Results match expected values
- [ ] Export functions work (PDF, Excel, CSV)
- [ ] Real-time features work (SignalR)

---

## Rollback Plan

If migration fails or critical issues arise:

### Immediate Rollback (Within 24 Hours)

1. **Restore v3 Database**

   ```sql
   RESTORE DATABASE TallyJ3
   FROM DISK = 'C:\Backups\TallyJ3.bak'
   WITH REPLACE;
   ```

2. **Reactivate v3 Application**
   - Restart v3 web server
   - Update DNS to point to v3 server
   - Notify users

3. **Communicate Issue**
   - Email all users
   - Explain rollback reason
   - Provide new timeline

### Delayed Rollback (After 24 Hours)

1. **Export v4 Data Entered Since Migration**
   - New elections created
   - New ballots entered

2. **Restore v3**

3. **Manually Re-enter v4 Data into v3**

4. **Plan Second Migration Attempt**
   - Address issues from first attempt
   - Test more thoroughly
   - Schedule new migration window

---

## Support and Resources

### Documentation

- [User Guide](USER_GUIDE.md) - End-user documentation
- [Admin Guide](ADMIN_GUIDE.md) - System administration
- [Deployment Guide](DEPLOYMENT.md) - Installation and deployment

### Migration Assistance

- Email: support@tallyj.com (if applicable)
- GitHub Issues: [TallyJ v4 Repository](https://github.com/your-org/tallyj4)
- Community Forum: [Link if available]

### Known Issues

**Issue:** Some v3 elections have NULL dates  
**Workaround:** Set a default date during migration

**Issue:** v3 ballot codes don't match v4 format  
**Workaround:** Regenerate ballot codes in v4

**Issue:** Users must reset passwords  
**Workaround:** Use admin panel to create temporary passwords

---

## Appendix: Field Mapping

### Election Entity

| v3 Field              | v4 Field                   | Notes                |
| --------------------- | -------------------------- | -------------------- |
| Id (int)              | ElectionGuid (guid)        | Primary key change   |
| Name                  | Name                       | Same                 |
| DateOfElection (date) | DateOfElection (datetime2) | Type change          |
| ElectionType (int)    | ElectionType (string)      | Enum to string       |
| NumberToElect         | NumberToElect              | Same                 |
| NumberExtra           | NumberExtra                | Same                 |
| Status (int)          | TallyStatus (string)       | Enum to string       |
| OwnerId               | OwnerGuid                  | References User GUID |

### Person Entity

| v3 Field              | v4 Field              | Notes               |
| --------------------- | --------------------- | ------------------- |
| Id (int)              | PersonGuid (guid)     | Primary key change  |
| ElectionId (int)      | ElectionGuid (guid)   | Foreign key change  |
| FirstName             | FirstName             | Same                |
| LastName              | LastName              | Same                |
| CanVote (bit)         | CanVote (bit)         | Same                |
| CanReceiveVotes (bit) | CanReceiveVotes (bit) | Same                |
| \_FullName (computed) | \_FullName (computed) | Both auto-generated |

---

**Last Updated:** February 2, 2026  
**Version:** 4.0.0
