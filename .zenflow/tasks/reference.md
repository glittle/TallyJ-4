# Technical Reference: TallyJ System Components

## Database Schema Reference

### Overview

TallyJ uses Entity Framework 6.4.4 Code First with SQL Server. The database schema contains **16 core entities** plus ASP.NET Identity tables.

---

### Core Entities

#### 1. Election

**Purpose**: Stores election configuration and metadata

| Column                 | Type     | Nullable | Description                                                          |
| ---------------------- | -------- | -------- | -------------------------------------------------------------------- |
| C_RowId                | int      | No       | Primary key (auto-increment)                                         |
| ElectionGuid           | Guid     | No       | Unique identifier for election                                       |
| Name                   | string   | Yes      | Election display name (e.g., "New York Ridván 2026")                 |
| Convenor               | string   | Yes      | Responsible Assembly/organization name                               |
| DateOfElection         | DateTime | Yes      | Election date (Gregorian)                                            |
| ElectionType           | string   | Yes      | Type code (e.g., "LSA", "Con", "Unit")                               |
| ElectionMode           | string   | Yes      | Mode code (e.g., "N" for Normal, "T" for Tie-break)                  |
| NumberToElect          | int      | Yes      | Number of positions to fill (ballot size)                            |
| NumberExtra            | int      | Yes      | Number of near-winners to report                                     |
| LastEnvNum             | int      | Yes      | Last envelope number assigned                                        |
| TallyStatus            | string   | Yes      | Current workflow state (NotStarted, NamesReady, Tallying, Finalized) |
| ShowFullReport         | bool     | Yes      | Display full results?                                                |
| OwnerLoginId           | string   | Yes      | Username of election creator/owner                                   |
| ElectionPasscode       | string   | Yes      | Access code for tellers                                              |
| ListedForPublicAsOf    | DateTime | Yes      | When listed on public home page                                      |
| C_RowVersion           | byte[]   | Yes      | Optimistic concurrency token                                         |
| ListForPublic          | bool     | Yes      | Show on public election list?                                        |
| ShowAsTest             | bool     | Yes      | Mark as test election?                                               |
| UseCallInButton        | bool     | Yes      | Enable "Called In" voting method?                                    |
| HidePreBallotPages     | bool     | Yes      | Skip gathering ballots phase?                                        |
| MaskVotingMethod       | bool     | Yes      | Hide how people voted?                                               |
| OnlineWhenOpen         | DateTime | Yes      | When online voting opens                                             |
| OnlineWhenClose        | DateTime | Yes      | When online voting closes                                            |
| OnlineCloseIsEstimate  | bool     | No       | Is close date/time firm or estimated?                                |
| OnlineSelectionProcess | string   | Yes      | Online voting mode                                                   |
| OnlineAnnounced        | DateTime | Yes      | When online voting was announced                                     |
| EmailFromAddress       | string   | Yes      | From address for voter emails                                        |
| EmailFromName          | string   | Yes      | From name for voter emails                                           |
| EmailText              | string   | Yes      | Email template for voters                                            |
| SmsText                | string   | Yes      | SMS template for voters                                              |
| EmailSubject           | string   | Yes      | Email subject line                                                   |
| CustomMethods          | string   | Yes      | Custom voting method names (comma-separated)                         |
| VotingMethods          | string   | Yes      | Enabled voting methods (comma-separated flags)                       |
| Flags                  | string   | Yes      | Additional configuration flags (JSON)                                |

**Indexes**: ElectionGuid (unique), OwnerLoginId, TallyStatus, DateOfElection

**Key Relationships**:

- One-to-many with Person (voters)
- One-to-many with Ballot
- One-to-many with Location
- One-to-many with Result
- One-to-many with JoinElectionUser

---

#### 2. Person

**Purpose**: Stores voter/candidate information for an election

| Column               | Type     | Nullable | Description                                       |
| -------------------- | -------- | -------- | ------------------------------------------------- |
| C_RowId              | int      | No       | Primary key                                       |
| ElectionGuid         | Guid     | No       | Foreign key to Election                           |
| PersonGuid           | Guid     | No       | Unique identifier for this person record          |
| LastName             | string   | Yes      | Surname                                           |
| FirstName            | string   | Yes      | Given name                                        |
| OtherLastNames       | string   | Yes      | Maiden name, alternate surnames                   |
| OtherNames           | string   | Yes      | Middle names, alternate first names               |
| OtherInfo            | string   | Yes      | Additional identifying information                |
| Area                 | string   | Yes      | Sector/geographic area                            |
| BahaiId              | string   | Yes      | Bahá'í ID number                                  |
| CombinedInfo         | string   | Yes      | Searchable combined text (computed)               |
| CombinedSoundCodes   | string   | Yes      | Soundex codes for fuzzy matching                  |
| CombinedInfoAtStart  | string   | Yes      | Original combined info (before edits)             |
| AgeGroup             | string   | Yes      | Age category (for youth eligibility)              |
| CanVote              | bool     | Yes      | Eligible to vote in this election?                |
| CanReceiveVotes      | bool     | Yes      | Eligible to be voted for?                         |
| IneligibleReasonGuid | Guid     | Yes      | Reason for ineligibility                          |
| RegistrationTime     | DateTime | Yes      | When registered at Front Desk                     |
| VotingLocationGuid   | Guid     | Yes      | Foreign key to Location                           |
| VotingMethod         | string   | Yes      | How they voted (InPerson, Online, MailedIn, etc.) |
| EnvNum               | int      | Yes      | Envelope number assigned                          |
| C_RowVersion         | byte[]   | Yes      | Concurrency token                                 |
| C_FullName           | string   | Yes      | Computed: "LastName, FirstName"                   |
| C_RowVersionInt      | long     | Yes      | Integer version number                            |
| C_FullNameFL         | string   | Yes      | Computed: "FirstName LastName"                    |
| Teller1              | string   | Yes      | Name of teller who registered them                |
| Teller2              | string   | Yes      | Name of assisting teller                          |
| Email                | string   | Yes      | Email address for online voting                   |
| Phone                | string   | Yes      | Phone number for online voting/SMS                |
| HasOnlineBallot      | bool     | Yes      | Has submitted online ballot?                      |
| Flags                | string   | Yes      | Additional person flags (JSON)                    |
| UnitName             | string   | Yes      | Unit/cluster name                                 |
| KioskCode            | string   | Yes      | Code for kiosk voting                             |

**Indexes**: ElectionGuid + PersonGuid (unique), ElectionGuid + Email, ElectionGuid + Phone, BahaiId

---

#### 3. Ballot

**Purpose**: Represents a physical or digital ballot envelope

| Column              | Type   | Nullable | Description                              |
| ------------------- | ------ | -------- | ---------------------------------------- |
| C_RowId             | int    | No       | Primary key                              |
| LocationGuid        | Guid   | No       | Foreign key to Location (where entered)  |
| BallotGuid          | Guid   | No       | Unique identifier for ballot             |
| StatusCode          | string | Yes      | Status (OK, Review, Spoiled, etc.)       |
| ComputerCode        | string | Yes      | Code of computer that entered it         |
| BallotNumAtComputer | int    | No       | Sequential number at that computer       |
| C_BallotCode        | string | Yes      | Computed: Computer + Number (e.g., "A3") |
| Teller1             | string | Yes      | Name of teller who entered ballot        |
| Teller2             | string | Yes      | Name of assisting teller                 |
| C_RowVersion        | byte[] | Yes      | Concurrency token                        |

**Indexes**: LocationGuid, BallotGuid (unique), ComputerCode

**Key Relationships**:

- Many-to-one with Location
- One-to-many with Vote

---

#### 4. Vote

**Purpose**: Individual vote on a ballot (one name)

| Column                  | Type   | Nullable | Description                                 |
| ----------------------- | ------ | -------- | ------------------------------------------- |
| C_RowId                 | int    | No       | Primary key                                 |
| BallotGuid              | Guid   | No       | Foreign key to Ballot                       |
| PositionOnBallot        | int    | No       | Position (1-9 for LSA elections)            |
| PersonGuid              | Guid   | Yes      | Foreign key to Person (candidate voted for) |
| StatusCode              | string | Yes      | Valid, Invalid, Extra, etc.                 |
| InvalidReasonGuid       | Guid   | Yes      | Reason if invalid                           |
| SingleNameElectionCount | int    | Yes      | Count for single-name elections             |
| C_RowVersion            | byte[] | Yes      | Concurrency token                           |
| PersonCombinedInfo      | string | Yes      | Snapshot of person's name (in case changed) |
| OnlineVoteRaw           | string | Yes      | Original text from online ballot            |

**Indexes**: BallotGuid, PersonGuid, StatusCode

**Key Relationships**:

- Many-to-one with Ballot
- Many-to-one with Person (PersonGuid)

---

#### Additional Core Entities

- **Location**: Voting locations/polling stations
- **Teller**: Election workers/administrators
- **Result**: Tallied election results (vote counts per person)
- **ResultSummary**: Summary statistics for election results
- **ResultTie**: Tie-break ballot information
- **JoinElectionUser**: Links authenticated users to elections with roles
- **OnlineVoter**: Voter authentication records (one-time code login)
- **OnlineVotingInfo**: Submitted online ballots
- **ImportFile**: CSV imports (voter lists, ballot imports)
- **Message**: System messages and notifications
- **C_Log**: System activity log (audit trail)
- **SmsLog**: SMS message delivery tracking

---

## Business Logic Reference

### Tally Algorithm Overview

TallyJ implements sophisticated ballot tallying logic to count votes, detect ties, rank candidates, and generate election results. The tally system handles various election types (LSA elections, conventions, by-elections, tie-breaks) with specific Bahá'í electoral rules.

---

### Core Tally Flow

#### Step 1: Trigger Tally Analysis

**Entry Point**: `/After/Analyze` → `AfterController.StartAnalysis()`

**Process**:

1. User clicks "Analyze Ballots" button
2. Server creates appropriate analyzer based on election type
3. Analysis runs (can take 30 seconds to 5 minutes for large elections)
4. Results saved to database
5. UI redirected to reports page

#### Step 2: Analyzer Selection

| Election Mode | Analyzer Class               | Description                                        |
| ------------- | ---------------------------- | -------------------------------------------------- |
| Normal        | `ElectionAnalyzerNormal`     | Standard elections                                 |
| SingleName    | `ElectionAnalyzerSingleName` | Single position (e.g., tie-break for one position) |

#### Step 3: Count Votes

**Algorithm**:

1. Iterate through all **valid ballots** (StatusCode = Ok)
2. For each ballot, iterate through all **valid votes** (VoteStatusCode = Ok)
3. For each vote: Find or create `Result` record for that candidate, increment `VoteCount` by 1
4. Progress updates via SignalR every 10 ballots

**Vote Validity**:

- **Ok**: Valid vote, counts toward tally
- **Spoiled**: Person ineligible
- **Changed**: Person info changed after ballot entry (needs review)
- **OnlineRaw**: Online ballot not yet processed

#### Step 4: Finalize Results and Detect Ties

**Actions**:

1. Sort results by vote count (descending)
2. Assign ranks (1 = highest votes)
3. Detect ties: Candidates with same vote count = tied
4. Categorize results: Elected, Extra, Other
5. Detect "close to" relationships
6. Determine tie-break requirements

#### Step 5: Finalize Summaries

Create or update `ResultSummary` record with statistics

#### Step 6: Save to Database

Persist all `Result` records and `ResultSummary`

---

### Tie-Break Elections

**Purpose**: Resolve ties from previous election

**Special Handling**:

- Only candidates in the tie group are on the ballot
- Uses same tally logic as normal election
- Results update original election's tie records

---

### Single-Name Elections

**Use Case**: Elect single position (e.g., tie-break)

**Difference from Normal**:

- Each "ballot" can have **multiple votes for same person**
- `Vote.SingleNameElectionCount` field used
- Tally counts total votes per person across all ballots
- When voters and tellers are co-located, tellers will often do this count by hand. If in different locations, use this and make one ballot for each location.

---

### Bahá'í Electoral Principles

1. **Secret Ballot**: No link between `Ballot` and `Person` (voter)
2. **No Nominations**: All eligible persons are candidates (marked as "Can be Voted for")
3. **Plurality Voting**: Each vote counts equally (1 point)
4. **Tie-Breaking**: Ties resolved by additional election. Head teller can manually record the tie winner.
5. **Confidentiality**: Vote counts not disclosed until election finalized

---

## Security Reference

### Authentication Systems Overview

TallyJ implements **three completely independent authentication systems** to serve different user types. These systems DO NOT share authentication mechanisms, user databases, or session management.

---

### System 1: Admin Authentication (Username + Password + Optional 2FA)

#### Purpose

Authenticates **system administrators and election owners** who need full access to create elections, configure settings, and manage the system.

#### User Database

- **Table**: `AspNetUsers` (ASP.NET Identity 2.2.4)
- **Storage**: SQL Server database
- **Management**: Admin accounts must be created by system administrators

#### Authentication Flow

1. **Login Page**: `/Account/LogOn`
2. **Credential Validation**: ASP.NET Membership Provider
3. **Session Establishment**: OWIN Cookie Authentication
4. **Claims Stored**: UserName, UniqueID (A: prefix), IsKnownTeller, IsSysAdmin

#### Session Management

- **Technology**: OWIN Cookie Authentication
- **Cookie**: `.AspNet.Cookies` (HttpOnly, Secure, SameSite)
- **Expiration**: 7 days
- **Session State**: StateServer (TCP on localhost:42424)

#### Future improvements

- Add Google Account login.

---

### System 2: Teller Authentication (Access Code)

#### Purpose

Authenticates **election tellers** (workers) who need access to enter ballots and manage elections.

#### User Database

- **Table**: `Teller` (custom table)
- **Storage**: SQL Server database
- **Management**: Tellers created on successful login and is associated to the computer used.

#### Authentication Flow

1. **Access Code Entry**: Tellers enter election passcode
2. **Code Validation**: Match against `Election.ElectionPasscode`
3. **Session Creation**: Create teller session with computer registration
4. **Computer Registration**: Link teller to specific computer/code

#### Session Management

- **Technology**: Custom session management
- **Storage**: StateServer (TCP on localhost:42424)
- **Scope**: Election-specific
- **Computer Tracking**: Each computer gets unique code

---

### System 3: Voter Authentication (One-Time Code)

#### Purpose

Authenticates **voters** for online ballot submission without requiring accounts.

#### User Database

- **Table**: `OnlineVoter` (custom table)
- **Storage**: SQL Server database
- **Management**: Voter records created during registration

#### Authentication Flow

1. **Registration**: Voter provides email or phone. If that is found in a person record in a currently open election, the one-time code can be created and sent.
2. **Code Generation**: System generates one-time verification code
3. **Code Delivery**: Send via email or twilio (SMS or voice call)
4. **Code Verification**: Voter enters code to authenticate
5. **Ballot Access**: Authenticated voter can submit ballot

#### Code Management

- **Storage**: `OnlineVoter.VerifyCode` field
- **Expiration**: Time-based (VerifyCodeDate)
- **Attempts**: Limited attempts (VerifyAttempts)
- **Security**: Codes are single-use, time-limited

---

### Security Architecture Patterns

#### Multi-Tenant Isolation

- Elections are completely isolated
- No cross-election data access
- User roles scoped to specific elections

#### Authorization Levels

1. **Anonymous**: Public voter registration
2. **Voter**: Online ballot submission (System 3)
3. **Teller**: Ballot entry (System 2)
4. **Admin**: Full election access to all associated elections (System 1)
5. **Super Admin**: Access to system-wide reports.

#### Session Security

- **Admin Sessions**: OWIN cookies with security flags
- **Teller Sessions**: StateServer with election scoping
- **Voter Sessions**: Code-based, no persistent sessions

#### Data Protection

- **Audit Logging**: All authentication attempts logged
- **Access Control**: Role-based permissions throughout

---

### Integration Points

#### OAuth Providers (Admin System)

- **Facebook**: Configured in Web.config
- **Google**: Configured in Web.config
- **External Logins**: Stored in AspNetUserLogins

#### SMS Provider (Voter System)

- **Twilio**: Configured for voter notifications
- **Delivery Tracking**: SmsLog table records all messages
- **Status Monitoring**: Success/failure tracking

#### Email System (Election Communications)

- **Templates**: Stored in Election table
- **Bulk Sending**: Automated voter notifications
- **Tracking**: Message table for delivery status

#### Logging Integrations

- **IFTTT**: External webhook notifications
- **LogEntries**: Cloud logging service
- **Local Logging**: C_Log table for audit trail
