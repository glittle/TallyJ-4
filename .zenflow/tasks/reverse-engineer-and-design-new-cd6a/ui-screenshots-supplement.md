# TallyJ Additional Screenshots Analysis (Batch 2)

**11 new critical screenshots captured**

---

## 15. Election Setup - Step 1: Define the Election
**URL**: `tallyj.com/Setup`
**Phase**: Setting Up (orange)
**Navigation**: Configure this Election | Import Name List | Edit People's Names | Send Notifications

### Jump Links
- Step 2 - Listing
- Step 3 - Features  
- Step 4 - Online

### Form Fields

| Field | Value/Options | Description |
|-------|---------------|-------------|
| **Type of Election** ⓘ | Unit Convention dropdown | Election category |
| **Variation** ⓘ | Normal Election dropdown | Specific election type |
| **Spaces on Ballot** ⓘ | 2 | Number of people to elect |
| **Report on next highest** ⓘ | 3 | Show near-winners in reports |
| **Convenor** ⓘ | Test 1 | Name of responsible Assembly |
| **Name in TallyJ** ⓘ | Test 123 | Displayed for tellers/voters; may be viewed publicly |
| **Date of Election** ⓘ | 2019-08-13 (date picker) | Optional for LSA elections; confirms 13th day of Glory month |
| **Just Testing?** ⓘ | ⦿ Yes ○ No | Show as Test election in list |

### Instructions (Blue panel)
1. Core settings (in gray) locked once ballots entered
2. Provide name for election list
3. Other tellers see this name; listed on public TallyJ home page while active
4. Name should include locality and type (e.g., "New York Ridván 2026")
5. Date optional but helps confirm correct date (13th day of Glory/Jalál) for LSA elections; shown in reports

### Bahá'í Calendar Integration
- Sunset calculation: "Sunset in your area (51.10 N, 113.95 W) was at **9:03 pm on 13 August**"
- Shows both Gregorian and Bahá'í calendar dates:
  - Started before sunset? → **13 Perfection (Kamál) 176**
  - Started after sunset? → **14 Perfection (Kamál) 176**

### Save Changes Button (top right)

---

## 16. Election Setup - Step 2: List the election for tellers
**Continued from Step 1**

### Instructions
1. Tellers without system login can join as 'guest' tellers using access code

### Form Fields

| Field | Options | Description |
|-------|---------|-------------|
| **Allow Guest Tellers?** ⓘ | ○ Yes ⦿ No | If Yes, shown on Home Page for guest tellers to join |
| **Access Code** ⓘ | [setsetst] | Assistant tellers use this code; can be changed later on Monitor Progress page |
| **Can Add People?** ⓘ | ⦿ Yes ○ No | When guest teller finds spoiled vote, can they add new name? If not, logged-in teller must add |

### Details
- Access code only effective if an Access Code is set
- Can be changed on Monitor Progress page later

---

## 17. Election Setup - Step 3: Configure Features (Top)
**Continued from Steps 1-2**

### Use "Gathering Ballots"? ⓘ
- ⦿ Yes ○ No
- Use TallyJ to register voters or record collection of ballot envelopes?

### Use "Roll Call"?
**Yes** (selected with green highlight):
- As voters arrive, validate identity at tellers' stations and mark as **In Person**
- Later, project **Roll Call** and have people publicly deposit ballots when name displayed

**No**:
- Voters come to tellers' station with ballot
- Validate identity, accept ballot, mark as **In Person**

**Note**: For all processes, tellers can accept absentee (**Dropped Off** and **Mailed In**) ballots at any time

---

## 18. Election Setup - Step 3: Configure Features (Bottom)
**Same page, scrolled down**

Shows same content as Screenshot #9 (previously documented):
- How will ballots be received? (checkboxes)
- Checklist Items
- Show Envelope Numbers?
- Multiple locations
- Time display format
- Email From Name/Address

---

## 19. Roll Call Display - Settings Panel
**URL**: `tallyj.com/Before/RollCall`
**Phase**: Gathering Ballots (blue)
**Navigation**: Front Desk | Sort Envelopes | **Roll Call** | Count Envelopes

### Display Options Panel

**Settings**:
- **Highlight those who voted at**: dropdown (appears empty) | * are recommended
- **Highlight**: all voting methods * dropdown
- **Show voter's**: home area * dropdown
- ☑ **Show ballot delivery methods** *

### Instructions
**To return** to these instructions: Press Home or scroll back to top
**To view in full-screen mode**: Press F11
**To back up** to the previous name: Press Up or K
**To move quickly through the list**: Press Up/Down, PgUp/PgDn
**To advance** to the next name: Press Space, Enter, ↓, Down Arrow or Click any name in the list

**Button**: "Start Roll Call Now"

---

## 20. Roll Call Display - Full Screen (Projector Mode)
**URL**: `tallyj.com/Before/RollCall`
**Display**: Large format for public viewing

### Voter Cards Display

Four large cards showing voter names:

1. **Little, Glen** (bordered in black - currently highlighted)
   - Badge: "Online" (blue, top right)

2. **Little, Glen**
   - Badge: "Online" (blue)
   - Secondary badge: "Test" (yellow/green)

3. **1** (number) **Little, Priscilla**
   - Badge: "Test 2" (beige/tan, top right)

4. **Little, Stephanie**
   - Badge: "Online" (blue, top right)

### Design Features
- **Minimal UI**: No navigation, just names
- **Large typography**: Names clearly visible from distance
- **Current voter highlighted**: Black border around active card
- **Ballot method badges**: Color-coded voting method (Online, Test, Test 2)
- **Sequential numbering**: Some voters show a number (1) before name
- **Clean white background**: Maximum legibility
- **TallyJ logo**: Small logo in bottom right

### Keyboard Navigation
Press Space/Enter/↓ to advance to next voter

---

## 21. Monitor Progress Page
**URL**: `tallyj.com/After/Monitor`
**Phase**: Processing Ballots (green)
**Navigation**: Monitor Progress | Enter Ballots | Import External Ballots | Analyze Ballots | View Reports | Display Tie-Breaks

### Location Status Table

| Location | Ballots % | Counted | Entered | Status | Computers | Contact Info |
|----------|-----------|---------|---------|--------|-----------|--------------|
| **B** | - | 0 | 1 🔗 | - | Code: A, Ballots: 1, Current Tellers |
| **C** | - | 0 | 0 🔗 | - | Code: -, Ballots: -, Current Tellers |
| **Online** | 100% | 3 | 3 🔗 | - | Code: OL, Ballots: 3, Current Tellers |
| **Main Location** | 200% | 1 | 2 🔗 | - | Code: A, Ballots: 2, Current Tellers |

### Teller Access
- ☐ Open this election to allow other tellers to participate
- Teller access code is: **bettetst**

### Auto-refresh
- ☑ Auto-refresh this page every: minute dropdown
- "Refreshed at 10:24:17 PM **(a few seconds ago)**"
- **Refresh Now** button

### Online Voting Section

**Status**: "Expected to close in a few seconds" (orange badge)

**Show as**: ⦿ Expected ○ Firm

**Datetime picker**: 2026-01-01 22:24:55 (closing day and time)

**Quick Actions**:
- Schedule close in 30 minutes
- Schedule close in 5 minutes
- Close now

**Processing Status**: "0 ballots ready to process after Online Voting is closed"

**Button**: "Process Online Ballots that are Submitted" (blue)

### Voter Table

| Voter ↕ | Email ↕ | Phone ↕ | Online Ballot ↕ | Front Desk Registration ↕ | When ↕ |
|---------|---------|---------|-----------------|---------------------------|--------|
| Little, Glen | glen.little2@gmail.c... | - | Processed ☐ | Online ☐ | Online: 2019 Oct 18, 8:56 pm<br>Front Desk: 12:56 am, Online |
| Little, Glen | - | - | Processed ☐ | Online ☐ | Online: 2021 Apr 3, 11:01 am<br>Front Desk: Apr 3 -3:00 PM, google-oauth2, Online |
| Little, Priscilla | glittle@calgarybahai... | +14034027106 | Draft ⚠ | Test 2 ☐ | Online: 2025 Jan 11, 6:37 pm<br>Front Desk: 9:13 AM, Test 2, Glen |
| Little, Stephanie | glen.little@gmail.com | - | Processed ☐ | Online ☐ | Online: 2021 Apr 5, 5:11 am<br>Front Desk: Apr 5 -9:11 AM, email, Online |

### Key Features
- Real-time location progress tracking
- Percentage completion per location
- Computer codes for teller stations
- Online voting countdown with quick close options
- Voter status with color coding (Processed = green, Draft = red/pink)
- Email and phone contact info
- Registration timestamps and methods
- Teller names responsible for registration

---

## 22. System Administration - Elections List
**URL**: `tallyj.com/SysAdmin`
**Tab**: Elections List (highlighted in green)
**Other tabs**: General Log | Online Voting | Unconnected Voters | Home

### Filters
- **Within a date range**: [Start date] To [End date] + 🔍 Search button
- **Sorting**: DateOfElection_Date ↓
- **Auto-refresh**: (no auto refresh) dropdown
- **Loaded**: a minute ago

### Elections Table (Partial view - 19 visible rows)

| # | Date | Recent Activity | Test? | Election | Convenor | Admin Email(s) | Type | Status | Ballot Size | People | Online | Ballots |
|---|------|-----------------|-------|----------|----------|----------------|------|--------|-------------|--------|--------|---------|
| 1904 | 2026 Apr 21 | 2025 Oct 13, 8:21 am | Test | LSA Melville Test Election | Melville LSA | Faranakalilo@gmail.com [Owner] | LSA / N | Tallying | 9 | 242 | 2 | 8 |
| 1955 | 2026 Apr 07 | 2025 Dec 02, 3:19 am | Test | Logan LSA Election | LSA of Logan | rammankashani@gmail.com [Owner] | LSA / N | Finalized | 9 | 182 | 1 | 3 |
| 1894 | 2026 Mar 18 | 2025 Jul 17, 2:30 am | Test | LSA of Bayswater | Bayswater | r.aghdasi@ecu.edu.au [Owner] | LSA / N | NotStarted | 9 | 0 | 0 | 0 |
| 1969 | 2026 Feb 06 | 2025 Dec 29, 2:09 pm | Test | 2026 WCA U21 | Local Spiritual Assembly of Albany, WA | albany@wa.bahai.org.au [Owner] | Con / N | NotStarted | 9 | 152 | 0 | 0 |
| ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... |

**Key columns**:
- **Date**: Election date
- **Recent Activity**: Last modification timestamp
- **Test?**: Test election flag
- **Election**: Election name
- **Convenor**: Responsible organization
- **Admin Email(s)**: Owner contact with [Owner] tag, [Full] tag for additional admins
- **Type**: LSA/N (Normal), Con/N (Convention), LSA/B (By-election), etc.
- **Status**: Tallying, Finalized, NotStarted, NamesReady
- **Ballot Size**: Number of positions to fill
- **People**: Registered voters count
- **Online**: Online voters count
- **Ballots**: Entered ballots count

### Scope
Shows elections from **worldwide**, dating back to 2025 and forward to 2026. System-wide administrative view.

---

## 23. System Administration - General Log
**URL**: `tallyj.com/SysAdmin`
**Tab**: General Log (highlighted in green)

### Filters
- **Filter by Detail**: text input
- **Filter by Who**: text input
- **Within a date range**: [Start date] To [End date] + 🔍 Search
- **Show 50 lines**: dropdown
- **Auto-refresh**: (no auto refresh) dropdown
- **Loaded**: 22 seconds ago

### Activity Log Table (50+ rows visible)

| When | Election | Site | Comp | Voter | Details |
|------|----------|------|------|-------|---------|
| 2026 Jan 01, 10:24 pm | Test 123 | WIN16A / tallyj.com / 3.5.41 | A | A.glen | Status changed to Tallying |
| 2026 Jan 01, 10:23 pm | Test 123 | WIN16A / tallyj.com / 3.5.41 | A | A.glen | Status changed to NamesReady |
| 2026 Jan 01, 10:22 pm | Test 123 | WIN16A / tallyj.com / 3.5.41 | A | A.glen | Status changed to NotStarted |
| 2026 Jan 01, 10:20 pm | Test 123 | WIN16A / www.tallyj.com / 3.5.41 | - | V.+14034027106 | Voter login via sms +14034027106 |
| 2026 Jan 01, 10:19 pm | Test 123 | WIN16A / tallyj.com / 3.5.41 | A | A.glen | Teller (Glen) switched into Election (Comp A) |
| 2026 Jan 01, 10:19 pm | - | WIN16A / tallyj.com / 3.5.41 | - | A.glen | Admin Logged In - glen (glen.little@gmail.com) |
| 2026 Jan 01, 10:19 pm | - | WIN16A / tallyj.com / 3.5.41 | - | V.+14034027106 | Log Out |
| 2026 Jan 01, 10:17 pm | - | WIN16A / tallyj.com / 3.5.41 | - | V.+14034027106 | Voter login via sms +14034027106 |
| 2026 Jan 01, 10:07 pm | Test 123 | WIN16A / tallyj.com / 3.5.41 | A | A.glen | Status changed to Tallying |
| ... (many more rows) |

### Log Entry Types Observed
- Status changed to {Tallying, NamesReady, NotStarted}
- Voter login via {sms, email} +{phone} / {email}
- Teller (Name) switched into Election (Comp X)
- Admin Logged In - {name} ({email})
- Log Out
- Email: Ballot Submitted
- Submitted Ballot
- Verify Code Sent via email
- Invalid voter signin code

### Site Information Format
`{ServerName} / {Domain} / {Version}`
Example: `WIN16A / tallyj.com / 3.5.41`

### Scope
**Global system log** showing all activity across all elections, all users, all actions. Essential for:
- Troubleshooting
- Security auditing
- User activity tracking
- System health monitoring

---

## 24. System Administration - Online Voting Tab
**URL**: `tallyj.com/SysAdmin`
**Tab**: Online Voting (highlighted in green)

### Filters (same as Elections List)
- Date range search
- Sorting: DateOfElection_Date ↓
- Auto-refresh dropdown
- Loaded: 14 seconds ago

### Elections Table (Online Voting Focus)

| Election | Convenor | Admin Email(s) | Ballot Size | Tally Status | Logged In | Pending | Processed | Open Date | First Activity | Most Recent | Close Date |
|----------|----------|----------------|-------------|--------------|-----------|---------|-----------|-----------|----------------|-------------|------------|
| 2026 WCA U21 | Local Spiritual Assembly of Albany, WA | albany@wa.bahai.org.au [Owner] | 9 | NotStarted | 0 | 0 | 0 | 2025 Dec 29, 1:21 pm | - | - | 2026 Feb 07, 3:00 am |
| Unit Convention 2026 | Tea Tree Gully | bayanma69@hotmail.com [Owner], chadmauger@gmail.com [Full] | 1 | NotStarted | 4 | 4 | 0 | 2025 Dec 30, 11:40 pm | 2025 Dec 31, 12:11 am | 2026 Jan 01, 3:22 am | 2026 Feb 06, 6:30 pm |
| Whitehorse Spiritual Assembly | Whitehorse | Whitehorse@vic.bahai.org.au [Owner] | 1 | Tallying | 20 | 19 | 0 | 2025 Dec 20, 4:00 pm | 2025 Dec 20, 4:04 pm | 2026 Jan 01, 3:08 pm | 2026 Feb 12, 1:30 am |
| Unit 1 Elections (Amman) | Amman LSA | unit.convention.elections@gmail.com [Owner] | 11 | NamesReady | 33 | 25 | 0 | 2025 Dec 29, 9:00 am | 2025 Dec 29, 10:12 am | 2026 Jan 01, 1:24 pm | 2026 Jan 09, 6:30 am |
| Test 123 | Test 1 | glen.little@gmail.com [Owner] | 2 | Tallying | 4 | 0 | 3 | 2025 Jan 11, 6:07 pm | 2019 Oct 16, 8:56 pm | 2025 Jan 11, 6:37 pm | 2026 Jan 01, 10:24 pm |
| ... (more rows) |

### Key Metrics
- **Logged In**: Users who have authenticated
- **Pending**: Draft ballots not yet submitted
- **Processed**: Completed and submitted ballots
- **Open Date**: When online voting opened
- **First Activity**: First voter action
- **Most Recent**: Last voter activity
- **Close Date**: When voting closes/closed

### Use Cases
- Monitor online voting activity across all elections
- Track participation rates
- Identify elections with pending ballots
- See voting windows (open → close dates)
- Admin contact information for each election

---

## 25. System Administration - Unconnected Voters
**URL**: `tallyj.com/SysAdmin`
**Tab**: Unconnected Voters (highlighted in green)

### Purpose
Shows voters who registered in TallyJ but aren't connected to any election. Helps identify:
- Orphaned accounts
- Registration errors
- Voters who need to be added to elections

### Table Columns
| Rowid | Email | Phone | Country | When Registered | When Last Login |
|-------|-------|-------|---------|-----------------|-----------------|
| 3802 | - | +10785294461 | - | 2026 Jan 01, 1:20 pm | - |
| 3801 | - | +00962785294461 | - | 2026 Jan 01, 1:19 pm | - |
| 3800 | nouraldeenalassar@gmail.com | - | - | 2026 Jan 01, 1:14 pm | 2026 Jan 01, 1:21 pm |
| 3798 | - | +61483297496 | - | 2026 Jan 01, 3:19 am | 2026 Jan 01, 3:19 am |
| 3796 | vafapa@yahoo.com | - | - | 2026 Jan 01, 3:17 am | - |
| ... (70+ rows visible) |

### Data Points
- **Rowid**: Unique voter identifier
- **Email**: Email address (if registered via email)
- **Phone**: Phone number (if registered via SMS)
- **Country**: Country code (mostly empty in this view)
- **When Registered**: First registration timestamp
- **When Last Login**: Most recent login (empty if never logged in)

### Sorting
- Sorting: C_RowId ↓ (newest first)

### Insights
- Mix of email and phone registrations
- Some voters logged in after registration, others never returned
- International phone numbers (various country codes)
- Timestamps range from 2025 Sep to 2026 Jan
- Useful for cleanup and voter assistance

---

## 26. Display Tellers' Report - Awaiting Analysis
**URL**: `tallyj.com/After/Presenter`
**Phase**: Processing Ballots (green) → Finalized ⓘ (Attempted but not complete)
**Navigation**: Monitor Progress | Enter Ballots | Import External Ballots | Analyze Ballots | View Reports | **Display Tie-Breaks**
**Viewing**: Finalized / Display Tellers' Report

### Display State

**Message** (centered, gray text):
"**Analysis not complete**

The results will be available when the tellers finish their tallying and analysis."

### Purpose
- **Projector mode** for public display of official results
- Only shows results after Analyze Ballots process completes
- Prevents premature disclosure of results
- Clean, simple interface suitable for large screen display

---

## 27. Analyze Ballots Page ⭐ CRITICAL
**URL**: `tallyj.com/After/Analyze`
**Election**: "Toronto Test" - Unit Convention (for 16 members)
**Phase**: Processing Ballots (green)
**Navigation**: Monitor Progress | Enter Ballots | Import External Ballots | **Analyze Ballots** | View Reports | Display Tie-Breaks
**Viewing**: Processing Ballots / Analyze Ballots

### Critical Warnings (Red)

**Top Banner**:
"**Issues highlighted in red below must be resolved before election can be finalized for reporting.**

Please note that the Analysis must be run after any ballot is modified."

### Counts Section

**Tabs**: Counts | Calculated | Override | Final

**Eligible Voters**:
- Calculated: 1621
- Final: 1621

**Voter Counts (from Front Desk and/or manual)**:
- In Person: 0 → 0
- Dropped Off: 0 → 0
- Mailed In: 0 → 0
- Imported: 6 → 6
- **Total Voters**: 6 → 6

**Ballots Processed (after opening envelopes)**:
- Valid Ballots: 0 → 0
- Spoiled: 6 (ⓘ +) → 6
- **Total Ballots**: 6 → 6
- Spoiled Votes ⓘ: 0 → 0

**Warning** (Red): "**Analysis is needed.**"

### Ballot Issues Section (Red)

"**These ballots need to be fixed before results can be determined**"

List of problematic ballots (all links clickable):
- IM1 (Imported) - Raw - Teller to Finish
- IM2 (Imported) - Raw - Teller to Finish
- IM3 (Imported) - Raw - Teller to Finish
- IM4 (Imported) - Raw - Teller to Finish
- IM5 (Imported) - Raw - Teller to Finish
- IM6 (Imported) - Raw - Teller to Finish

### Action Buttons

**Primary Actions**:
- **Re-run Analysis** (button)
- **Save Values** (button)
- **Show Analysis Log** (button)

**State Control**:
- **Set Election State**:
  - ○ Finalized
  - ⦿ Not Finalized

### Instructions Panel (Right side)

**Steps**:
1. If there are any errors shown in red, they need to be resolved
2. Once all are resolved, review the results carefully and determine if they should be approved
3. To indicate that the results are approved, set the "Election Status" to "Finalized" to enable the "Display Teller's Report" page and other reports to be viewed correctly

### Key Quality Control Features

**Pre-finalization Checks**:
- Voter count validation
- Ballot count matching
- Spoiled ballot identification
- Raw/unprocessed ballot detection
- Vote validation

**Workflow**:
1. Enter all ballots
2. Run analysis
3. Fix issues highlighted in red
4. Re-run analysis
5. Verify counts match expected
6. Set to "Finalized" when clean
7. Reports become available

**Safety Mechanisms**:
- Cannot finalize with errors
- Must explicitly set status
- Analysis log available
- Override capability (Calculated vs Final columns)
- Clear error messaging

### Data Integrity
- Prevents incomplete results from being published
- Ensures all imported ballots are processed
- Validates voter vs ballot counts
- Tracks spoiled votes separately
- Links directly to problem ballots for fixing

---

## Summary of New Screenshots

### Setup & Configuration (4)
✅ Election Setup Step 1 - Core settings (type, variation, date, convener, testing flag)  
✅ Election Setup Step 2 - Teller access configuration  
✅ Election Setup Step 3 (top) - Gathering Ballots & Roll Call options  
✅ Election Setup Step 3 (bottom) - Ballot methods, locations, email settings

### Projector/Display Modes (2)
✅ Roll Call - Settings panel with display options  
✅ Roll Call - Full screen display (large voter cards with voting method badges)

### Monitoring & Administration (5)
✅ Monitor Progress - Location tracking, online voting controls, voter table  
✅ SysAdmin - Elections List (worldwide elections table)  
✅ SysAdmin - General Log (global activity log)  
✅ SysAdmin - Online Voting tab (participation metrics)  
✅ SysAdmin - Unconnected Voters (orphaned accounts)

### Quality Control & Reporting (2)
✅ **Analyze Ballots** - Critical pre-finalization validation ⭐  
✅ Display Tellers' Report - Awaiting analysis state

---

## Screenshots Now Total: 28

### Complete Workflows Documented
- ✅ Voter journey (login → select election → cast ballot)
- ✅ Election setup (Steps 1-4 complete)
- ✅ Voter registration (CSV import & manual entry)
- ✅ Ballot collection (Front Desk, Roll Call)
- ✅ Ballot entry (teller interface)
- ✅ Quality control (**Analyze Ballots** - the gatekeeper!)
- ✅ Results & reports
- ✅ System administration (4 admin tabs)
- ✅ Projector modes (Roll Call display)
