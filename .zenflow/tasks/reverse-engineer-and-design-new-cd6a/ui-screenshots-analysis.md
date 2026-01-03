# TallyJ UI/UX Analysis from Screenshots

## Overview
Analysis of 14 unique screenshots showing the complete TallyJ election workflow from voter perspective, teller perspective, and administrator perspective.

---

## 1. Landing Page / Home (Public)
**URL**: `tallyj.com`

### Layout
- **Header**: TallyJ logo with "Bahá'í Election System" tagline, version 3.5.41
- **Main Section**: Centered card with "What would you like to do?"

### Primary Actions
1. **Vote Online** (Orange button)
   - For individuals voting in an election
   - Opens modal dialog

2. **Join as a Teller** (Blue button)
   - For tellers with access code to assist in registering voters and tallying ballots
   - Opens modal dialog

3. **Elect Officers** (Gray button)
   - For Assemblies and committees to elect their own officers at first meeting

4. **Manage Elections** (Gray button)
   - For head tellers to create and administer elections for their community

### Footer
- Feedback link to Glen Little
- Status and Feedback Document link
- About TallyJ, Reference Materials, Policies & Agreements links
- Copyright © 2026 Glen Little
- Version 3.5.41
- Social media icons (Facebook, Twitter/X)

---

## 2. Vote Online Modal
**Triggered by**: Clicking "Vote Online" on landing page

### Content
- **Title**: "Vote Online"
- **Welcome message**: "How would you like to log in to TallyJ to vote?"
- **Link**: "Learn about voting with TallyJ with the Voter's Guide"

### Authentication Options
1. **Email** (Green circular icon)
   - "Using your email"
   
2. **Phone** (Orange circular icon)
   - "Using your phone"

### Important Notice Box
- Email/phone must match what's registered in election
- If election not found after login, contact Assembly or convenor
- User Agreement and Privacy Policy acceptance required

---

## 3. Join as a Teller Modal
**Triggered by**: Clicking "Join as a Teller" on landing page

### Content
- **Title**: "Assist as a Teller"
- **Subtitle**: "Join an election as a teller to assist in registering voters and tallying ballots"

### Form Fields
1. **Select your election** (Dropdown)
   - Shows: "(No elections are currently open for tellers.) [None Active]"
   - Has "Refresh" button

2. **Type in the tellers' access code** (Text input)

3. **Join the election** (Blue button)
   - "Join Now"

---

## 4. Election Setup - Step 4: Online Voting
**URL**: `tallyj.com/Setup`
**Election State**: Setting Up (orange phase)
**Sub-navigation**: Configure this Election | Import Name List | Edit People's Names | Send Notifications

### Use "Roll Call" Configuration
- **Yes** (Radio): Validate identity at tellers' stations, mark as "In Person", later project Roll Call for public ballot deposit
- **No** (Radio): Tellers accept ballots at station, validate identity, mark as "In Person"
- **Note**: Tellers can accept absentee ballots (Dropped Off and Mailed In) at any time

### How will ballots be received? (Checkboxes)
- ☑ In Person
- ☑ Dropped Off
- ☑ Mailed In
- ☐ Called In
- ☑ Online *
- ☑ Online Kiosk *
- ☐ Imported **
- ☐ Test 2

**Notes**:
- 3 extra custom methods available
- To accept Online ballots, select in Step 4
- ** Imported automatically checked when ballots imported
- Kiosk only available if Online also active (see Online Voting link)

### Enable this browser as a Kiosk?
- Yes / No buttons

### Checklist Items
- Two text input fields with add/delete buttons
- "Add Another" button
- Sample uses: "Attending", "Lunch"

### Show Envelope Numbers?
- ⦿ All but "In Person"
- ○ All
- ○ None
- Purpose: For whom should envelope numbers be shown on Front Desk?

### Multiple locations?
- ⦿ Yes
- ○ No
- Question: Are tellers collecting ballots in multiple locations/polling stations?
- **Locations list**:
  - B (with up/down/delete buttons)
  - C (with up/down/delete buttons)
  - Main Local (with up/down/delete buttons)
  - "Add a Location" button with note "(Use short, meaningful names)"

### Time display format
- ○ 19:30
- ⦿ 7:30 pm

### Email From Name
- Text field: "Test"
- Description: Name shown on From address for voter emails

### Email From Address
- Text field: "glittle@calgary-bahai.org"
- Description: From address for election emails (visible to all voters)

### Save Changes Button (top right)

---

## 5. Import CSV - Name List Upload
**URL**: `tallyj.com/Setup/ImportCsv`
**Election**: "Test 123" - Unit Convention (for 2 members)
**Navigation**: Setting Up | Import Name List
**Top bar**: Admin | Tellers ✗ | Online Voting ✗

### Instructions Panel
1. Prepare comma-separated CSV file with people's names
2. Can use Excel or export from membership system
3. File must have "headers row" with column titles (no accents like Bahá'í)
4. Save as normal Windows text/CSV or UTF-8/UTF-16 encoding
5. Usually contains eligible adults list
6. Must have First and Last name; optional fields: Bahá'í ID, area, maiden name, etc.
7. TallyJ displays columns for field mapping
8. May need to adjust CSV and re-import if accented characters don't process correctly

### Step 1: Upload the CSV file
- **Upload a file** button
- Drag and drop support

### Files on the Server Table
| Action | Status | Name | Upload Time | Headers on Line | Content Encoding | Size | Other Actions |
|--------|--------|------|-------------|-----------------|------------------|------|---------------|
| (selected) | Uploaded | 2023-11-07-Ontario-Voter-List.csv | 2023 Nov 7, 1:45 pm | 1 ▼ | UTF-8 ▼ | 566058 | ✕ 🗑 |

### Step 2: Map columns
Number of columns in CSV: 10

**Column Mapping Interface**:
- CSV headers shown: Bahá'í ID, S, G, LastName, FirstName, MiddleName, FormerName, Nickname, U
- TallyJ fields shown with dropdowns: (ignore), (ignore), Last Name ▼, First Name ▼, (ignore) ▼, (ignore) ▼, (ignore) ▼
- Sample data preview showing names like Aalaei, Aazami with Bahá'í IDs

"Hide TallyJ Fields" button

**Field Descriptions**:
- First Name: Required. The individual's first/given name

### Step 3: Import people
**Eligibility Status panel** showing valid reasons and recommended settings for:
- Local Assembly election
- Tie-break election  
- By-election
- National Convention or 2nd stage local Assembly election

Eligibility categories:
- **Eligible**: Anyone with no entry
- **Can Vote and be Voted For**: Eligible
- **Cannot Vote, Cannot be Voted For**:
  - Under 18 years old
  - Resides elsewhere
  - Moved elsewhere recently
  - Not in this local unit
  - Deceased
  - Not a delegate and on other Institution
  - Not a registered Bahá'í
  - Rights removed (entirely)
  - Other (cannot vote or be voted for)
- **Can Vote (but cannot be voted for)**:
  - Youth aged 18/19/20
  - On other Institution (e.g. Counsellor)
  - By-election: On Institution already
  - Tie-break election: Not tied
  - Rights removed (cannot be voted for)
  - Other (can vote but not be voted for)
- **Cannot Vote (but can be voted for)**:
  - Not a delegate in this election
  - Rights removed (cannot vote)
  - Other (cannot vote but can be voted for)

**Import Button**: "Import now"
**Status**: 4 people | Delete all People Records

---

## 6. Edit People's Names Page
**URL**: `tallyj.com/Setup/People`
**Election**: "Test 123" - Unit Convention (for 2 members)
**Navigation**: Setting Up > Edit People's Names

### Search and Filter
- **Search box**: "4 people on file" | "Show All" button | "Add New Person" button
- **Person list**:
  - ⊗ Little, Glen (Test)
  - Little, Glen (highlighted in green)
  - ⊗ Little, Priscilla
  - Little, Stephanie

### Person Details Panel (Right side)
**Voting Method**: Online

**Activity Log**:
- 8:48 am - Online
- 12:56 am - Cancel Online
- 12:56 am - Online

**Form Fields**:
- First Name: Glen (with info icon)
- Last Name: Little (with info icon)
- Eligibility status: "Eligible to vote and be voted for" dropdown (with info icon)
  - Can vote in this election?: Yes
  - Can be voted for in this election?: Yes
- Other First Name(s): (with info icon)
- Other Last Name(s): (with info icon)
- Other Identifying Information: (with info icon)
- Sector / Area: (with info icon)
- Bahá'í ID: (with info icon)
- Email Address: glen.little2@gmail.com (with info icon)
- Phone Number: Sample: +15873281844 (with info icon)

**Save Changes** button (blue)

---

## 7. Send Notifications Page
**URL**: `tallyj.com/Setup/Notify`

### Testing Section
**Left column**: "Test the email first to see what it looks like by sending it just to yourself!"
- "Send the email to 0 voters..." button
- "Copy 0 email addresses..." text (For BCC in an external email)

**Right column**: "Test the SMS first to see what it looks like by sending it just to yourself!"
- "Send the SMS message to 0 voters..." button

**Message Log**: "Check the log below to see if the messages get delivered"

### Notification History
**Closed**: a year ago - Thu, Jan 16, 2025 5:46 AM

**Filter buttons**:
- All | None | Not Voted | Voted Online | Unfinished Online Ballot | Only has Email | Only has SMS | Refresh List

### Voter Table
| Voter ↕ | Email ↕ | Phone ↕ | Front Desk ↕ | Online Ballot ↕ |
|---------|---------|---------|--------------|-----------------|
| Little, Glen | glen.little2@gmail.com | - | Online (green) | Processed (green) |
| Little, Glen | - | - | Online (green) | Processed (green) |
| Little, Priscilla | glittle@calgarybahai.ca | +14034027106 | Test 2 (beige) | Draft (pink) |
| Little, Stephanie | glen.little@gmail.com | - | Online (green) | Processed (green) |

### Recent Message Log
| Person / Action | When | Time |
|----------------|------|------|
| Email: Announcement sent to 1 person | 9 months ago | Tue, Apr 15, 2025 7:57 AM |
| Email: Announcement sent to 0 people (1 failed to send, SMTP error...) | 9 months ago | Tue, Apr 15, 2025 7:38 AM |
| Email: Announcement sent to 1 person | 10 months ago | Mon, Feb 17, 2025 8:54 PM |
| ... (many more entries) |
| Priscilla Little +14034027106 delivered | 10 months ago | Mon, Feb 17, 2025 7:53 PM |
| Sms: Sent to 1 person in 1 second | 10 months ago | Mon, Feb 17, 2025 7:53 PM |
| Sms: Sending to 1 person (see above) | 10 months ago | Mon, Feb 17, 2025 7:53 PM |
| ... (older entries from 2021-2023) |

**Buttons**: Download Complete Log | Refresh Log

---

## 8. Front Desk Page
**URL**: `tallyj.com/Before/FrontDesk`
**Election**: "Test 123" - Unit Convention (for 2 members)
**State**: Gathering Ballots (blue phase)
**Navigation**: Front Desk | Sort Envelopes | Roll Call | Count Envelopes

### Top Controls
- **Location**: B dropdown | ⓘ
- **Teller at Keyboard**: Glen dropdown | ⓘ
- **Assisting**: Which Teller? dropdown
- **Which Teller?** dropdown (red background - missing selection)

**Hide Instructions & Tips** button

### Instructions
1. To quickly find a person, type name or Bahá'í ID in Search Box, use Up/Down arrows, press Enter to focus
2. Count of people by status shown at top; click to filter; press Esc to remove filter
3. If using Roll Call, mark people as "In Person" when they arrive; they can cast ballot when Roll Call is done
4. "Ballot Not Received" hides everyone whose ballot has been received; combine with Checklist item to see whose ballot has not been received

### Search and Status Filters
- **Search box**: (Esc to clear)
- **Status counts**: Online: 2 | Mailed In: 0 | Dropped Off: 0 | In Person: 0 | Total: 2 | Other: 0
- **Checkbox**: ☐ Ballot Not Received

### Voter List
| Voting Method | Name | Registration Info |
|---------------|------|-------------------|
| Online (green badge) | **Little, Glen** | 2019 Oct 18, 8:56 pm, Online 🔗 |
| Online (green badge) | Mailed In / Dropped Off / In Person (gray badges) | **Little, Priscilla** | 2021 Apr 5, 5:13 am, Glen, Main Location 🔗 |
| Online (green badge) | **Little, Stephanie** | 2021 Apr 5, 5:11 am, Glen, Online 🔗 |

**Second screenshot** shows Little, Glen selected (highlighted in yellow)

---

## 9. Ballot Entry Page  
**URL**: `tallyj.com/Ballots`
**Election**: "Test 123" - Unit Convention (for 2 members)
**State**: Gathering Ballots → **Processing Ballots** (green phase)
**Navigation**: Monitor Progress | Enter Ballots | Import External Ballots | Analyze Ballots | View Reports | Display Tie-Breaks

### Top Controls (Red background - requires selection)
- **Location**: B dropdown | ⓘ
- **Teller at Keyboard**: Glen dropdown | ⓘ
- **Assisting**: Which Teller? dropdown
- **Which Teller?** dropdown

**Hide Instructions & Tips** button

### Instructions
1. Each paper ballot needs to be recorded here
2. Ballot automatically saved as votes added or modified
3. Duplicate names will be noticed and marked
4. Update Location's information to indicate counting status; update Contact Info if head teller needs to contact you
5. To delete a ballot, first delete all votes on the ballot

### Left Panel - Ballot Entry
**Status Indicators**:
- ▸ Location Status: Unknown
- ▸ Ballots - B
- ▸ Add votes to Ballot # A3

**Search for a person**: ⓘ
- Input field with ↓
- "Use ↓ keys to move in the list. Press Enter to add."
- Person list:
  - ✓ Little, Glen (green checkmark)
  - Little, Priscilla (strikethrough)
  - Little, Stephanie
  - Little, Glen (strikethrough with tooltip "Ineligible: On other Institution (e.g. Counsellor) - Test")

**Action Buttons**:
- Close Ballot
- Reload Ballot 🔄
- Start New Ballot

**Checkbox**: ☐ If this ballot needs review by the head teller, tick here

### Right Panel - Current Ballot
**Header**: Too Few
**Ballot #**: A3

**Names on the ballot**:
1. Little, Glen (with + and 🗑 icons)
2. (empty row)

---

## 10. Simple Results Report
**URL**: `tallyj.com/After/Reports#SimpleResults`
**Election**: "Test 123" - Unit Convention (for 2 members)  
**State**: Processing Ballots (green) → **Finalized** ⓘ
**Navigation**: View Reports

### Left Sidebar - Report Menu
**Ballot Reports**:
1. Main Election Report
2. Tellers' Report, by Votes
3. Tellers' Report, by Name
4. Ballots (All for Review)
5. Ballots (Online Only)
6. Ballots (For Tied)
7. Spoiled Votes
8. Ballot Alignment
9. Duplicate Ballots
10. Ballots Summary

**Voter Reports**:
1. Can Be Voted For
2. Participation
3. Attendance Checklists
4. Voted Online
5. Eligible and Voted by Area
6. Voting Method by Venue
7. Attendance by Venue
8. Updated People Records
9. With Eligibility Status
10. Email & Phone List

**Print (Ctrl+P)** button

Note: Some browsers can create PDF when printing

### Main Report Area
**Warning** (red): "The election is not Finalized in TallyJ. This report may be incomplete and/or showing wrong information."

**Election Details**:
- Test 123
- Convener: Test 1
- Date: 13 August 2019

**Statistics**:
- Eligible voters: 3
- Voted: 4
- **Ballots received ≠ Voted**: 6 (in red - discrepancy)
- Percentage of participation: 200%
- Did not vote: -1

**Ballot Distribution**:
- Ballots cast in person: 0
- Ballots received by mail: 0
- Ballots hand-delivered: 0
- Ballots cast online: 3
- Ballots: Test 2: 1

**Ballot Issues**:
- Spoiled ballots: 2
  - 1 - Empty
  - 1 - Too Few
- Spoiled votes: 4
  - 3 - On other Institution (e.g. Counsellor)
  - 1 - Resides elsewhere

### Elected Table
| # | Name | Bahá'í Id | Votes |
|---|------|-----------|-------|
| 1 | Little, Glen | - | 2 |
| 2 | Little, Stephanie | - | 2 |

**Export Buttons**: Export Stats | Export Elected

---

## 11. Dashboard Page
**URL**: `tallyj.com/Dashboard`

### Header Section
- TallyJ logo (left)
- Navigation tabs/sections visible

### Main Content - Phase Tiles

#### Gathering Ballots - Registration and Voting
**Tiles**:
1. **Front Desk**
   - Centrally monitor progress of tellers (with projector icon 🖥)

2. **Sort Envelopes**

3. **Roll Call**
   - (with projector icon 🖥)

4. **Count Envelopes**

#### Processing Ballots - Tallying and Analyzing
**Tiles**:
1. **Monitor Progress**
   - Centrally monitor progress of tellers

2. **Enter Ballots**
   - Type in the names found on ballots

3. **Import External Ballots**
   - Import ballots collected via an external source or method (with projector icon 🖥)

4. **Analyze Ballots**
   - Review results, checking for ties (with cursor/hand icon)

5. **View Reports**
   - Generate election reports

6. **Display Tie-Breaks**
   - Show tie-break information (with projector icon 🖥)

7. **Display Tellers' Report**
   - (with projector icon 🖥)

#### Other Pages
**Tiles**:
1. **Elections List**
   - (with cursor/hand icon)

2. **Sys Logs**
   - (with cursor/hand icon)

3. **Reference Materials**

4. **Change my Password**
   - (with cursor/hand icon)

5. **Logout**

### Icons Legend
- 🖥 = Projector mode (for public display)
- ✋ = Requires teller login/authentication

---

## Key UI/UX Patterns Observed

### Design System
- **Colors**:
  - Orange: Primary actions, "Setting Up" phase, "Vote Online"
  - Blue: Secondary actions, "Gathering Ballots" phase, "Join as a Teller"
  - Green: Success states, "Processing Ballots" phase, online voting completed
  - Red: Errors, warnings, required selections
  - Yellow: Selected/highlighted items
  - Gray: Disabled or inactive items
  - Beige/tan: Alternative statuses

### Navigation Patterns
- **Phase-based workflow**: Setting Up → Gathering Ballots → Processing Ballots → Finalized
- **Breadcrumb-style tabs**: Show current phase and available actions
- **Dashboard tile navigation**: Large clickable cards organized by phase
- **Persistent header**: Election name, state, admin status, teller count

### Form Patterns
- **Multi-step wizards**: CSV import (Upload → Map → Import)
- **Modal dialogs**: Vote Online, Join as Teller
- **Inline help**: Info icons (ⓘ) with tooltips
- **Real-time validation**: Field-level feedback
- **Auto-save**: Ballots save automatically

### Data Display
- **Status badges**: Color-coded voting methods (Online, In Person, etc.)
- **Sortable tables**: Column headers with ↕ indicators
- **Filterable lists**: Multiple filter options (All, None, Not Voted, etc.)
- **Search with keyboard navigation**: Up/Down arrows, Enter to select
- **Expandable sections**: ▸ for collapsed, ▾ for expanded

### Accessibility Features
- Keyboard navigation support (Esc to clear, Enter to select, ↑↓ to navigate)
- Clear visual hierarchy
- Icon + text labels
- High contrast for important alerts
- Info tooltips for context

### Responsive Behavior
- Desktop-first design
- Modals for mobile-friendly dialogs
- Stacked layouts for narrow screens (implied from structure)

### Real-time Features
- SignalR for live updates (implied from architecture)
- Refresh buttons for manual updates
- Status synchronization across tellers
- Message delivery logging

---

## 12. Voter Portal - My Elections
**URL**: `tallyj.com/Vote`
**User**: Logged in as voter (authenticated via phone: +14034027106)

### Header
- **Title**: TallyJ - Bahá'í Election System
- **Action**: Log out button (top right)

### Background
- Inspirational garden image (red and yellow flowers)
- **Message overlay**: "Take time to meditate"

### Welcome Message
"Welcome! Your phone number is **+14034027106**. It is found in the **4 elections** shown below.

If an upcoming election is not listed here, it may not exist in TallyJ or your phone number is not registered in it. Please contact the head teller of the election for assistance."

### Status Notice (Yellow background)
"**None of these elections are available for you to vote in at this time.**  
When an election is available, you will be able to select it."

### My Elections Table

| Election | Online Voting | Your Name and Ballot |
|----------|---------------|---------------------|
| **Test 123**<br>13 Aug 2019 - Test 1<br>Unit Convention<br>Email for assistance | Closed a year ago | **Little, Priscilla**<br>Registered: Custom2<br><br>**Draft**<br>11 Jan 2025 6:37 pm<br><br>**Little, Glen**<br>Registered: Online<br><br>**Processed**<br>4 Apr 2020 12:36 pm<br><br>**Smith, John** |
| **Sample Election**<br>19 Apr 2020 - Glen<br>Unit Convention<br>Email for assistance | Closed 3 years ago | Registered: -<br><br>**Draft**<br>8 Apr 2022 9:28 am |
| **Glen - Test only**<br>(no date) - [Convener]<br>Local Spiritual Assembly | Closed 4 years ago | **Little, Glen**<br>Registered: - |
| **[New Election]**<br>11 Nov 2023 - [Convener]<br>Other | No online voting | **Little, Glen**<br>Registered: - |

### Key Information Displayed
For each election:
- **Election name**
- **Date and Convener**
- **Election type** (Unit Convention, Local Spiritual Assembly, Other)
- **Contact method** (Email for assistance)
- **Online voting status**:
  - "Closed X time ago" (past elections)
  - "No online voting" (elections without online voting enabled)
  - Would show "Open" or voting deadline for active elections
- **Voter's name(s)** in election (can be registered multiple times)
- **Registration type**: Online, Custom2, or - (in person)
- **Ballot status**:
  - **Draft** (started but not submitted) - with timestamp
  - **Processed** (submitted and counted) - with timestamp
  - Registered but not started - shows only name and registration type

### Activity Log Section
**Header**: "Activity"  
**Subheader**: "For your review and security, here is your recent activity (with the most recent first)." | **Refresh** button

**Activity Table**:
| Election | Action | When | Time |
|----------|--------|------|------|
| - | Voter login via sms +14034027106 | a few seconds ago | Thu, Jan 1, 2026 10:17 PM |

**Footer Note** (italicized):
"If something looks wrong, please stop using TallyJ and send details to your head teller and/or to global technical support."

### UX Observations

**Security Features**:
- Activity log shows all voter actions
- Phone number/email confirmation on login
- Timestamps for all ballot actions
- Security warning to report suspicious activity

**Status Communication**:
- Clear indication when voting is closed
- Yellow notice when no elections are currently open
- Different ballot states (Draft vs. Processed)
- Multiple registrations in same election shown separately

**Voter Guidance**:
- Instructions for missing elections
- Contact information (Email for assistance)
- Refresh capability for activity log
- Clear status messages

**Background Design**:
- Contemplative garden image
- "Take time to meditate" message
- Reinforces the spiritual nature of Bahá'í elections
- Creates calm, reflective atmosphere

**Multi-registration Support**:
- Same voter can appear multiple times in one election
- Different registration methods tracked (Online, Custom2, In Person)
- Separate ballot status per registration
- Useful for testing or special circumstances

---

## 13. Voter Portal - Active Election Available
**URL**: `tallyj.com/Vote`
**User**: Logged in as voter (phone: +14034027106)
**Status**: Election currently open for voting

### Election List - Active State

**Test 123 Election** (Orange highlight):
- **Status**: Open Now
- **Countdown**: Expected to close in 4 minutes (at 10:24 pm)
- **Action Button**: "Prepare my Ballot" (blue button)
- **Your Name**: Little, Priscilla / Little, Glen / Smith, John
- **Ballot Status**: Draft, Processed, etc.

**Other Elections** (Yellow background - closed):
- Sample Election: Closed 3 years ago
- Glen - Test only: Closed 4 years ago
- [New Election]: No online voting

### Activity Log (Expanded)
| Election | Action | When | Time |
|----------|--------|------|------|
| - | Voter login via sms +14034027106 | a few seconds ago | Thu, Jan 1, 2026 10:20 PM |
| - | Log Out | 8 minute ago | Thu, Jan 1, 2026 10:19 PM |
| - | Voter login via sms +14034027106 | 3 minutes ago | Thu, Jan 1, 2026 10:17 PM |

### Educational Resources Section
**Header**: "To help you prepare to vote, here are a few online resources about voting in Bahá'í elections:"

**Links**:
- Bahá'í Quotes on Elections (3 web pages)
- Bahá'í Quotes on Elections (1 page) [PDF icon]
- The Sanctity and Nature of Bahá'í Elections [new tab icon]
- How Bahá'í Voters Should Vote [new tab icon] by Arash Abizadeh

### Key Differences from Closed State
- **Orange banner** vs. yellow background for open election
- **Countdown timer** showing urgency
- **"Prepare my Ballot" button** (actionable) vs. "Closed" (informational)
- **Educational resources** appear when election is active
- **Expanded activity log** showing recent login/logout

---

## 14. Online Ballot Casting Interface
**URL**: `tallyj.com/Vote`
**Election**: Test 123 - Unit Convention (for 2 members)
**Voter**: Little, Priscilla
**Status**: Open Now - Expected to close in 4 minutes (at 10:24 pm)

### Header
- **Election name**: Test 123 / Little, Priscilla
- **Status banner** (Orange): "Open Now" / "Expected to close in 4 minutes (at 10:24 pm)"
- **Return button**: Navigate back to election list

### Left Panel: Add to your Ballot/Pool

**Purpose**: "Add a new person to your pool for this election."

**Form Fields**:
- **First name**: (text input - labeled "First")
- **Last name**: (text input - labeled "Last")
- **Extra identifying information**: (text input)
  - Helper text: "(Optional. Use this if the name may not be enough. But remember, your vote is anonymous so do not reference yourself in this description!)"

**Action Button**: "Add Person to my Ballot"

**Guidance Quote** (Blue italicized text):
"From among the pool of those whom the elector believes to be qualified to serve, selection should be made with due consideration given to such other factors as age distribution, diversity, and gender." - Universal House of Justice (view source)

### Right Panel: Voting for 2 People

**Registration Warning** (Red text):
"You are registered as having voted: **Test 2**

This online ballot below WILL NOT be used because tellers have registered you as voting: **Test 2**.

If that is not correct, please contact the head teller for this election before the election is closed!

Please add **2 people** before submitting your ballot."

**My Ballot Section**:
- **Subheader**: "(The order of names does not matter.)"
- **Ballot entries**:
  1. ✓ Test Test (green pill badge with "TEST X" - has remove button)
  2. (empty slot)

**Ballot State**: Currently has 1 of 2 required names

### Key Features Observed

**Validation & Constraints**:
- Requires exactly 2 people (based on election configuration)
- Shows count of how many more needed
- Order of names doesn't matter (anonymous ballot principle)

**Privacy Protection**:
- Warning not to include self-identifying information
- Emphasis on ballot anonymity
- Votes are anonymous

**Duplicate Handling**:
- System allows adding anyone (doesn't pre-validate eligibility)
- Duplicate detection happens server-side
- Name matching with "Extra identifying information" for disambiguation

**Status Conflicts**:
- Shows warning when teller has already registered vote via different method
- Allows voter to contact head teller if error
- Prevents double-voting

**UI/UX Patterns**:
- **Two-column layout**: Add on left, Review on right
- **Visual feedback**: Green pill badges for added names with X to remove
- **Countdown pressure**: Timer creates urgency
- **Inspirational quote**: Reinforces election principles
- **Clear instructions**: Step-by-step guidance
- **Error prevention**: Shows warnings before submission

**Accessibility**:
- Form labels clear
- Helper text for complex fields
- Color-coded warnings (red for critical)
- Quotes provide guidance inline

### Submission Flow (Inferred)
1. Voter adds names one at a time via left panel
2. Names appear in "My Ballot" section as green pills
3. Can remove names with X button
4. Once required number reached (2), can submit
5. System validates and processes ballot
6. Status changes from "Draft" to "Processed"

---

## Screenshots Captured (17 total)

### Public/Voter Screens (6)
✅ Landing page with 4 main options  
✅ Vote Online modal (Email/Phone login)  
✅ Join as a Teller modal  
✅ Voter portal - My Elections list (all closed)  
✅ Voter portal - Active election available (with "Prepare my Ballot" button)  
✅ **Online ballot casting interface** - The actual voting screen!

### Election Setup Screens (4)
✅ Step 4: Online Voting configuration  
✅ Import CSV - Upload and map columns  
✅ Import CSV - Eligibility status configuration  
✅ Edit People's Names - Person details form

### Teller Workflow Screens (5)
✅ Send Notifications - Email/SMS with delivery log  
✅ Front Desk - Voter registration/check-in  
✅ Ballot Entry - Enter votes from paper ballots  
✅ View Reports - Simple Results Report  
✅ Dashboard - Phase-based navigation tiles

### Additional Screens (2)
✅ Front Desk with voter selected (highlighted state)  
✅ Multiple duplicate screenshots showing various states

---

## Missing / Needed Screenshots

To complete full system documentation:

### High Priority
1. ✅ ~~**Online ballot casting interface**~~ - **CAPTURED!** Active voting screen where voters select names
2. **Election Setup - Steps 1-3** - Election name, type, date, number to elect, basic configuration
3. **Election List page** - How tellers choose which election to work on
4. **Analyze Ballots page** - Review results, tie-breaking interface
5. **Ballot submission confirmation** - What voter sees after submitting ballot

### Projector/Display Modes
6. **Roll Call display** - Public screen showing voters as they arrive
7. **Display Tie-Breaks** - Show tie information to assembly
8. **Display Tellers' Report** - Public results display

### Monitoring & Progress
9. **Monitor Progress page** - Real-time view of all teller stations' status

### Reports (18 remaining)
10. **Ballot Reports** (9 more):
   - Tellers' Report by Votes
   - Tellers' Report by Name
   - Ballots (All for Review)
   - Ballots (Online Only)
   - Ballots (For Tied)
   - Spoiled Votes
   - Ballot Alignment
   - Duplicate Ballots
   - Ballots Summary

11. **Voter Reports** (10):
    - Can Be Voted For
    - Participation
    - Attendance Checklists
    - Voted Online
    - Eligible and Voted by Area
    - Voting Method by Venue
    - Attendance by Venue
    - Updated People Records
    - With Eligibility Status
    - Email & Phone List

### Administration
12. **System Administration** (SysAdmin controller)
13. **Elections List for Admin** - Manage multiple elections
14. **User management** - Adding/removing tellers
15. **Election cloning/archiving**

### Communication
16. **Email templates** - Actual rendered emails voters receive
17. **SMS message examples** - What texts look like

### Error & Edge Cases
18. **Error states** - Validation failures, duplicate votes, network errors
19. **Mobile responsive views** - Phone/tablet layouts
20. **Offline/connection lost** states

### Optional but Helpful
21. **Sort Envelopes page**
22. **Count Envelopes page**
23. **Import External Ballots page**
24. **Reference Materials page**
25. **Sys Logs page**
