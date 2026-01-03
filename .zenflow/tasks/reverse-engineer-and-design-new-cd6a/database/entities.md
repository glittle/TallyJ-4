# TallyJ Database Entities Documentation

## Overview
TallyJ uses Entity Framework 6.4.4 Code First with SQL Server. The database schema contains **16 core entities** plus ASP.NET Identity tables.

---

## Core Entities

### 1. Election
**Purpose**: Stores election configuration and metadata

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key (auto-increment) |
| ElectionGuid | Guid | No | Unique identifier for election |
| Name | string | Yes | Election display name (e.g., "New York Ridván 2026") |
| Convenor | string | Yes | Responsible Assembly/organization name |
| DateOfElection | DateTime | Yes | Election date (Gregorian) |
| ElectionType | string | Yes | Type code (e.g., "LSA", "Con", "Unit") |
| ElectionMode | string | Yes | Mode code (e.g., "N" for Normal, "T" for Tie-break) |
| NumberToElect | int | Yes | Number of positions to fill (ballot size) |
| NumberExtra | int | Yes | Number of near-winners to report |
| LastEnvNum | int | Yes | Last envelope number assigned |
| TallyStatus | string | Yes | Current workflow state (NotStarted, NamesReady, Tallying, Finalized) |
| ShowFullReport | bool | Yes | Display full results? |
| OwnerLoginId | string | Yes | Username of election creator/owner |
| ElectionPasscode | string | Yes | Access code for tellers |
| ListedForPublicAsOf | DateTime | Yes | When listed on public home page |
| C_RowVersion | byte[] | Yes | Optimistic concurrency token |
| ListForPublic | bool | Yes | Show on public election list? |
| ShowAsTest | bool | Yes | Mark as test election? |
| UseCallInButton | bool | Yes | Enable "Called In" voting method? |
| HidePreBallotPages | bool | Yes | Skip gathering ballots phase? |
| MaskVotingMethod | bool | Yes | Hide how people voted? |
| OnlineWhenOpen | DateTime | Yes | When online voting opens |
| OnlineWhenClose | DateTime | Yes | When online voting closes |
| OnlineCloseIsEstimate | bool | No | Is close date/time firm or estimated? |
| OnlineSelectionProcess | string | Yes | Online voting mode |
| OnlineAnnounced | DateTime | Yes | When online voting was announced |
| EmailFromAddress | string | Yes | From address for voter emails |
| EmailFromName | string | Yes | From name for voter emails |
| EmailText | string | Yes | Email template for voters |
| SmsText | string | Yes | SMS template for voters |
| EmailSubject | string | Yes | Email subject line |
| CustomMethods | string | Yes | Custom voting method names (comma-separated) |
| VotingMethods | string | Yes | Enabled voting methods (comma-separated flags) |
| Flags | string | Yes | Additional configuration flags (JSON) |

**Indexes**: ElectionGuid (unique), OwnerLoginId, TallyStatus, DateOfElection

**Key Relationships**:
- One-to-many with Person (voters)
- One-to-many with Ballot
- One-to-many with Location
- One-to-many with Result
- One-to-many with JoinElectionUser

---

### 2. Person
**Purpose**: Stores voter/candidate information for an election

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| PersonGuid | Guid | No | Unique identifier for this person record |
| LastName | string | Yes | Surname |
| FirstName | string | Yes | Given name |
| OtherLastNames | string | Yes | Maiden name, alternate surnames |
| OtherNames | string | Yes | Middle names, alternate first names |
| OtherInfo | string | Yes | Additional identifying information |
| Area | string | Yes | Sector/geographic area |
| BahaiId | string | Yes | Bahá'í ID number |
| CombinedInfo | string | Yes | Searchable combined text (computed) |
| CombinedSoundCodes | string | Yes | Soundex codes for fuzzy matching |
| CombinedInfoAtStart | string | Yes | Original combined info (before edits) |
| AgeGroup | string | Yes | Age category (for youth eligibility) |
| CanVote | bool | Yes | Eligible to vote in this election? |
| CanReceiveVotes | bool | Yes | Eligible to be voted for? |
| IneligibleReasonGuid | Guid | Yes | Reason for ineligibility |
| RegistrationTime | DateTime | Yes | When registered at Front Desk |
| VotingLocationGuid | Guid | Yes | Foreign key to Location |
| VotingMethod | string | Yes | How they voted (InPerson, Online, MailedIn, etc.) |
| EnvNum | int | Yes | Envelope number assigned |
| C_RowVersion | byte[] | Yes | Concurrency token |
| C_FullName | string | Yes | Computed: "LastName, FirstName" |
| C_RowVersionInt | long | Yes | Integer version number |
| C_FullNameFL | string | Yes | Computed: "FirstName LastName" |
| Teller1 | string | Yes | Name of teller who registered them |
| Teller2 | string | Yes | Name of assisting teller |
| Email | string | Yes | Email address for online voting |
| Phone | string | Yes | Phone number for online voting/SMS |
| HasOnlineBallot | bool | Yes | Has submitted online ballot? |
| Flags | string | Yes | Additional person flags (JSON) |
| UnitName | string | Yes | Unit/cluster name |
| KioskCode | string | Yes | Code for kiosk voting |

**Indexes**: ElectionGuid + PersonGuid (unique), ElectionGuid + Email, ElectionGuid + Phone, BahaiId

**Key Relationships**:
- Many-to-one with Election
- Many-to-one with Location (VotingLocationGuid)
- One-to-many with Vote (as candidate)
- One-to-many with Result

---

### 3. Ballot
**Purpose**: Represents a physical or digital ballot envelope

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| LocationGuid | Guid | No | Foreign key to Location (where entered) |
| BallotGuid | Guid | No | Unique identifier for ballot |
| StatusCode | string | Yes | Status (OK, Review, Spoiled, etc.) |
| ComputerCode | string | Yes | Code of computer that entered it |
| BallotNumAtComputer | int | No | Sequential number at that computer |
| C_BallotCode | string | Yes | Computed: Computer + Number (e.g., "A3") |
| Teller1 | string | Yes | Name of teller who entered ballot |
| Teller2 | string | Yes | Name of assisting teller |
| C_RowVersion | byte[] | Yes | Concurrency token |

**Indexes**: LocationGuid, BallotGuid (unique), ComputerCode

**Key Relationships**:
- Many-to-one with Location
- One-to-many with Vote

---

### 4. Vote
**Purpose**: Individual vote on a ballot (one name)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| BallotGuid | Guid | No | Foreign key to Ballot |
| PositionOnBallot | int | No | Position (1-9 for LSA elections) |
| PersonGuid | Guid | Yes | Foreign key to Person (candidate voted for) |
| StatusCode | string | Yes | Valid, Invalid, Extra, etc. |
| InvalidReasonGuid | Guid | Yes | Reason if invalid |
| SingleNameElectionCount | int | Yes | Count for single-name elections |
| C_RowVersion | byte[] | Yes | Concurrency token |
| PersonCombinedInfo | string | Yes | Snapshot of person's name (in case changed) |
| OnlineVoteRaw | string | Yes | Original text from online ballot |

**Indexes**: BallotGuid, PersonGuid, StatusCode

**Key Relationships**:
- Many-to-one with Ballot
- Many-to-one with Person (PersonGuid)

---

### 5. Location
**Purpose**: Voting location/polling station or ballot collection point

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| LocationGuid | Guid | No | Unique identifier |
| Name | string | Yes | Location name (e.g., "Main Location", "B", "C") |
| ContactInfo | string | Yes | Phone/email for head teller contact |
| Long | string | Yes | Longitude (for maps) |
| Lat | string | Yes | Latitude (for maps) |
| TallyStatus | string | Yes | Counting status (Unknown, Counting, Ready, etc.) |
| SortOrder | int | Yes | Display order |
| BallotsCollected | int | Yes | Number of ballots collected at this location |

**Indexes**: ElectionGuid, LocationGuid (unique)

**Key Relationships**:
- Many-to-one with Election
- One-to-many with Person (voters registered at this location)
- One-to-many with Ballot (ballots entered at this location)

---

### 6. Teller
**Purpose**: Election worker/administrator record

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| Name | string | Yes | Teller's name |
| UsingComputerCode | string | Yes | Computer code currently using |
| IsHeadTeller | bool | Yes | Has head teller privileges? |
| C_RowVersion | byte[] | Yes | Concurrency token |

**Indexes**: ElectionGuid

**Note**: Tellers can be:
- Authenticated users (with account) - join via login
- Guest tellers (no account) - join via access code

**Key Relationships**:
- Many-to-one with Election

---

### 7. Result
**Purpose**: Tallied election results (vote counts per person)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| PersonGuid | Guid | No | Foreign key to Person |
| VoteCount | int | Yes | Number of votes received |
| Rank | int | No | Rank in results (1 = highest) |
| Section | string | Yes | Result section (Elected, Extra, Other) |
| CloseToPrev | bool | Yes | Close in count to previous person? |
| CloseToNext | bool | Yes | Close in count to next person? |
| IsTied | bool | Yes | Tied with other candidates? |
| TieBreakGroup | int | Yes | Tie-break group number |
| TieBreakRequired | bool | Yes | Needs tie-breaking? |
| TieBreakCount | int | Yes | Tie-break vote count |
| IsTieResolved | bool | Yes | Tie successfully broken? |
| RankInExtra | int | Yes | Rank within "Extra" section |
| ForceShowInOther | bool | Yes | Force display in "Other" section |

**Indexes**: ElectionGuid, PersonGuid, Rank

**Key Relationships**:
- Many-to-one with Election
- Many-to-one with Person

---

### 8. ResultSummary
**Purpose**: Summary statistics for election results

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| NumBallotsWithManual | int | Yes | Total ballots entered manually |
| NumBallotsWithOnline | int | Yes | Total online ballots |
| NumBallots | int | Yes | Total ballots |
| NumVoters | int | Yes | Total registered voters |
| NumVotersInPerson | int | Yes | In-person voters |
| NumVotersOnline | int | Yes | Online voters |
| SpoiledBallots | int | Yes | Number of spoiled ballots |
| InvalidVotes | int | Yes | Number of invalid votes |
| TotalVotes | int | Yes | Total votes cast |
| UsedManualEntry | bool | Yes | Manual entry was used? |
| UsedOnlineVoting | bool | Yes | Online voting was used? |
| ElectionDateText | string | Yes | Formatted election date |
| GuidSelectionsWithCodes | string | Yes | JSON of selections |

**Indexes**: ElectionGuid (unique)

**Key Relationships**:
- One-to-one with Election

---

### 9. ResultTie
**Purpose**: Stores tie-break ballot information

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| TieBreakGroup | int | No | Which tie group |
| NumInTie | int | No | Number of candidates tied |
| NumToElect | int | No | Number to elect from tie |
| TieBreakRequired | bool | No | Tie-breaking needed? |
| IsResolved | bool | No | Tie successfully broken? |

**Indexes**: ElectionGuid, TieBreakGroup

**Key Relationships**:
- Many-to-one with Election

---

### 10. JoinElectionUser
**Purpose**: Links authenticated users to elections with roles

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| UserId | Guid | No | Foreign key to AspNetUsers |
| Role | string | Yes | User's role (Admin, Teller, etc.) |
| InviteEmail | string | Yes | Email used for invitation |
| InviteWhen | DateTime | Yes | When invitation was sent |

**Indexes**: ElectionGuid, UserId

**Note**: This is for **authenticated users only** (System 1 authentication). Guest tellers (System 2) use access code without user account.

**Key Relationships**:
- Many-to-one with Election
- Many-to-one with AspNetUsers

---

### 11. OnlineVoter
**Purpose**: Stores voter authentication records (System 3 - one-time code login)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| VoterId | string | Yes | Email address or phone number |
| VoterIdType | string | Yes | "email" or "sms" |
| WhenRegistered | DateTime | Yes | First time registered |
| WhenLastLogin | DateTime | Yes | Most recent login |
| EmailCodes | string | Yes | Historical verification codes |
| Country | string | Yes | Country code (from phone or IP) |
| OtherInfo | string | Yes | Additional metadata |
| VerifyCode | string | Yes | Current verification code |
| VerifyCodeDate | DateTime | Yes | When code was generated |
| VerifyAttempts | int | Yes | Number of failed attempts |
| VerifyAttemptsStart | DateTime | Yes | When attempts window started |

**Indexes**: VoterId + VoterIdType (unique), VoterId

**Note**: No passwords! Voters authenticate via one-time codes sent to email/phone.

**Key Relationships**:
- No direct foreign keys (voters match to Person records via email/phone)

---

### 12. OnlineVotingInfo
**Purpose**: Stores submitted online ballots (drafts and completed)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| PersonGuid | Guid | Yes | Foreign key to Person (matched voter) |
| VoterId | string | Yes | Email or phone number |
| VoterIdType | string | Yes | "email" or "sms" |
| WhenSubmitted | DateTime | Yes | When ballot submitted (null = draft) |
| Choices | string | Yes | JSON array of candidate selections |
| Status | string | Yes | Draft, Submitted, Processed |
| ProcessedBallotGuid | Guid | Yes | Ballot created when processed |
| WhenProcessed | DateTime | Yes | When imported into tally |

**Indexes**: ElectionGuid, PersonGuid, VoterId + VoterIdType

**Key Relationships**:
- Many-to-one with Election
- Many-to-one with Person

---

### 13. ImportFile
**Purpose**: Tracks CSV imports (voter lists, ballot imports)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | No | Foreign key to Election |
| ImportType | string | Yes | "Voters", "Ballots", etc. |
| OriginalFileName | string | Yes | Uploaded file name |
| FirstDataRow | int | Yes | Which row contains data (after headers) |
| HasHeaders | bool | Yes | File has header row? |
| CodePage | string | Yes | Text encoding (UTF-8, UTF-16, etc.) |
| FileSize | long | Yes | Size in bytes |
| ColumnMappings | string | Yes | JSON mapping CSV columns to fields |
| NumProcessed | int | Yes | Number of records imported |
| WhenUploaded | DateTime | Yes | Upload timestamp |
| WhenProcessed | DateTime | Yes | Processing timestamp |
| ProcessedBy | string | Yes | Username who processed it |

**Indexes**: ElectionGuid, ImportType

**Key Relationships**:
- Many-to-one with Election

---

### 14. Message
**Purpose**: System messages and notifications

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| ElectionGuid | Guid | Yes | Foreign key to Election (if election-specific) |
| MessageType | string | Yes | Email, SMS, System, etc. |
| MessageTo | string | Yes | Recipient (email/phone/userId) |
| MessageText | string | Yes | Message content |
| WhenSent | DateTime | Yes | Send timestamp |
| Status | string | Yes | Sent, Failed, Pending |
| ErrorInfo | string | Yes | Error details if failed |

**Indexes**: ElectionGuid, WhenSent, Status

**Key Relationships**:
- Many-to-one with Election (optional)

---

### 15. C_Log
**Purpose**: System activity log (audit trail)

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| AsOf | DateTime | No | Log entry timestamp |
| ElectionGuid | Guid | Yes | Foreign key to Election (if applicable) |
| LocationGuid | Guid | Yes | Foreign key to Location (if applicable) |
| VoterId | string | Yes | Voter ID (for voter actions) |
| ComputerCode | string | Yes | Computer code (for teller actions) |
| Details | string | Yes | Log message |
| HostAndVersion | string | Yes | Server + version info |

**Indexes**: AsOf (descending), ElectionGuid, Details (full-text)

**Note**: Critical for troubleshooting and audit trail

**Key Relationships**:
- Many-to-one with Election (optional)
- Many-to-one with Location (optional)

---

### 16. SmsLog
**Purpose**: SMS message delivery tracking

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| C_RowId | int | No | Primary key |
| PhoneNumber | string | Yes | Recipient phone number |
| Message | string | Yes | SMS content |
| WhenSent | DateTime | Yes | Send timestamp |
| Status | string | Yes | Delivered, Failed, Pending |
| Provider | string | Yes | SMS provider (Twilio) |
| MessageId | string | Yes | Provider's message ID |
| Cost | decimal | Yes | Cost in USD |
| ErrorInfo | string | Yes | Error details if failed |

**Indexes**: PhoneNumber, WhenSent, Status

---

## ASP.NET Identity Tables

### AspNetUsers
**Purpose**: System administrator accounts (System 1 authentication)

| Column | Type | Description |
|--------|------|-------------|
| Id (UserId) | Guid | Primary key |
| UserName | string | Login username |
| Email | string | Email address |
| EmailConfirmed | bool | Email verified? |
| PasswordHash | string | Hashed password |
| SecurityStamp | Guid | Security token |
| PhoneNumber | string | Optional phone |
| PhoneNumberConfirmed | bool | Phone verified? |
| TwoFactorEnabled | bool | 2FA enabled? |
| LockoutEndDateUtc | DateTime | Account locked until |
| LockoutEnabled | bool | Can be locked out? |
| AccessFailedCount | int | Failed login attempts |

**Note**: Used ONLY for main admin authentication. Voters and guest tellers do NOT have accounts here.

### AspNetRoles
**Purpose**: Role definitions

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| Name | string | Role name |

### AspNetUserClaims
**Purpose**: User claims for authorization

### AspNetUserLogins
**Purpose**: External OAuth logins (Google, Facebook)

| Column | Type | Description |
|--------|------|-------------|
| LoginProvider | string | google-oauth2, facebook, etc. |
| ProviderKey | string | External user ID |
| UserId | Guid | Foreign key to AspNetUsers |

---

## Database Relationships Summary

### Core Relationships

```
Election (1) ─── (*) Person
Election (1) ─── (*) Location
Election (1) ─── (*) Ballot  ← via LocationGuid
Election (1) ─── (*) Result
Election (1) ─── (1) ResultSummary
Election (1) ─── (*) ResultTie
Election (1) ─── (*) JoinElectionUser ─── (*) AspNetUsers
Election (1) ─── (*) ImportFile
Election (1) ─── (*) OnlineVotingInfo
Election (1) ─── (*) Teller

Location (1) ─── (*) Person (VotingLocationGuid)
Location (1) ─── (*) Ballot

Ballot (1) ─── (*) Vote

Person (*) ─── (*) Vote (PersonGuid - as candidate)
Person (1) ─── (*) Result

OnlineVoter (*) ─── (*) Person (matched via email/phone, no FK)
OnlineVoter (*) ─── (*) OnlineVotingInfo (matched via VoterId)
```

---

## Key Design Patterns

### 1. Guid-based Foreign Keys
- All relationships use Guids, not integer IDs
- `C_RowId` is integer primary key for EF
- `{Entity}Guid` is business key for relationships

### 2. Soft Deletes
- No soft delete flags in current schema
- Data retention via archive tables (if any)

### 3. Optimistic Concurrency
- `C_RowVersion` byte array for conflict detection
- EF uses this for concurrency checks

### 4. Computed Columns
- `C_BallotCode` = ComputerCode + BallotNumAtComputer
- `C_FullName` = LastName + ", " + FirstName
- `CombinedInfo` = searchable concatenation of all person fields

### 5. Multi-tenancy by Election
- Almost all tables have `ElectionGuid`
- Elections are isolated from each other
- No cross-election queries except in SysAdmin

---

## Migration Considerations for .NET Core

### Changes Needed
1. **Entity Framework 6 → EF Core**
   - Keep same schema structure
   - Convert fluent API configurations
   - Keep Guid foreign keys
   - Maintain computed columns

2. **Nullable Reference Types**
   - Add `?` to nullable string properties
   - Ensure non-null columns are properly annotated

3. **Concurrency Tokens**
   - `[Timestamp]` attribute on `C_RowVersion`
   - Maintain optimistic concurrency behavior

4. **Indexes**
   - Recreate all indexes in EF Core
   - Add indexes for common query patterns
   - Consider full-text search on C_Log.Details

5. **Data Migration**
   - Create migration scripts from production schema
   - Test with full database backup
   - Validate all foreign key relationships
   - Test concurrency scenarios

---

## Entity Counts (Typical Production Scale)

| Entity | Small Election | Large Election | Notes |
|--------|----------------|----------------|-------|
| Election | 1 | 1 | Per election |
| Person | 50-200 | 10,000-50,000 | All eligible voters |
| Ballot | 30-150 | 5,000-30,000 | 60-80% turnout typical |
| Vote | 270-1350 | 45,000-270,000 | 9 votes per ballot (LSA) |
| Result | 50-200 | 10,000-50,000 | One per person |
| Location | 1-3 | 5-20 | Polling stations |
| Teller | 5-10 | 20-50 | Election workers |
| OnlineVoter | 20-100 | 5,000-30,000 | Online voting participants |
| C_Log | 100-500 | 10,000-100,000 | Activity log entries |

---

**End of Entity Documentation**
