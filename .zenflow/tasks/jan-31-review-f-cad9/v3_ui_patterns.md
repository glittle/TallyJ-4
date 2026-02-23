# TallyJ v3 UI Patterns Documentation

**Generated:** 2026-01-31  
**Source:** 70+ v3 screenshots analysis  
**Purpose:** Document actual v3 UI patterns to guide v4 implementation

---

## Core UI Philosophy

### State-Based Navigation

v3 uses a **workflow-based navigation system** where available actions change based on election state:

**Election States:**

1. **Setting Up** - Configuration, import, notifications
2. **Gathering Ballots** - Front desk, roll call, envelope sorting
3. **Processing Ballots** - Monitor, entry, import, analysis
4. **Finalized** - Reports and presentation

**UI Pattern:**

- Primary navigation shows current state (highlighted)
- Secondary navigation shows available actions for current state
- All states visible to admin, subset visible to assistant tellers
- Some actions available across multiple states

---

## 1. Election Configuration - Multi-Step Wizard

### Step 1: Define the Election

**Layout:** Single-page form with sections

**Core Settings:**

- Type of Election (dropdown: Unit Convention, LSA, etc.)
- Variation (dropdown: Normal Election, By-election, etc.)
- Spaces on Ballots (number input)
- Report on next highest (number input)
- Convenor (text input with description)
- Name to Use (text input for display name)
- Date of Election (date picker with Badi date calculation)
- Just Testing? (Yes/No radio)

**Instructions Section:**

- 5 numbered instructions at top
- "Hide Instructions & Tips" link to collapse
- Blue background box for instructions

### Step 2: List election for tellers

**Purpose:** Configure teller access

**Fields:**

- Allow Guest Tellers? (Yes/No radio with explanation)
  - If Yes: Shows election on public home page for guest teller login
  - Only effective if Access Code is set
- Access Code (text input labeled "testsetst" in example)
  - Used by tellers to join election
  - Can be changed from "Monitor Progress" page
- Can Add People? (Yes/No radio)
  - Controls whether guest tellers can add new names
  - Warning: Security consideration for guest tellers

### Step 3: Configure Features

**Section: "Gathering Ballots"**

**Use "Gathering Ballots"? (Yes/No radio)**

- If Yes: Use TallyJ to register voters or record collection of envelopes
- If No: As voters arrive, validate identity at tellers' stations and mark them as "In Person"

**Sub-options when "Yes":**

- Method selection (checkboxes for multiple):
  - Yes - As voters arrive at the election, validate their identity at tellers' stations and mark them as "In Person"
  - In Person
  - Dropped Off
  - Called In
  - Online
  - Kiosk\*
  - Imported\*\*

**Note boxes:**

- For all processes, tellers can accept absentee (Dropped Off and Mailed In) ballots at any time.
- To accept ballots Online using TallyJ's Online Voting, select it in Step 4 (below)."
- \*\* Imported is automatically checked when ballots are imported.
- \* Kiosk is only available if Online is also active. See 'Online Voting' for details of how to use it.

**Checklist Items:**

- Add custom items for Front Desk (text input)
- "Add Another" button
- Shows on "Attendance Checklist" report
- Examples: "Attending AGM", "Pre-payment", "Lunch", "Have they purchased a lunch ticket?"

**Show Envelope Numbers? (At/Not "In Person" radio)**

- Controls envelope numbering display on Front Desk

**Multiple locations? (Yes/No radio)**

- "Are tellers collecting ballots in multiple locations / polling stations for this election?"

**Time display format:** (dropdown: 12:30 / 7:30 pm)

**Email From Name:** (text input)

- "This will be the name on the From address. Use your name, another person's name, or the name of an agency that will be recognized by voters"
- Uses Election.EmailFromName field

**Email From Address:** (text input)

- "When emails are sent to voters regarding this election, this will be the From address. The address must be real."
- Uses Election.EmailFromAddress field

### Step 4: Online Voting

**Instruction Text:**

1. "This election can be made available so that voters can cast their ballots online."
2. "To vote online, the email address a voter uses to log in to TallyJ must match the email address in their record in this election."

**Enable Online Voting? (Yes/No radio)**

- Red warning when disabled: "Enable have been processed so you cannot disable Online Voting. You can change the Closing time to prevent receiving more ballots."
- Note: Once enabled and ballots received, cannot be disabled (only closed)

**Name Selection (Radio buttons A/B/C):**

**A: List** - Voters select from the people listed in this election

- Recommended when list is comprehensive
- Voters can vote for everyone or limited (e.g. a tie-break)
- Voters cannot add any new names
- Tellers will not need to find and enter the matching person
- High likelihood of spoiled votes if names entered incorrectly
- Note: If a voter adds a "random" name, tellers find and enter the matching person. There is a high likelihood of spoiled votes if the names entered incorrectly.

**B: Random** - Voters do not have a list to choose from but randomly type the names of the people they are voting for

- Recommended when you do not have a list or have a reason to not let voters use it
- Tellers will need to read the names based by reversing the names of the people entered by the voter
- They do not have a list to find the person
- Each vote is in a ballot, tellers find and enter the matching person
- There is a high likelihood of spoiled votes if the names entered incorrectly

**C: Both** - Voters are given a list of known people but have the option of adding any other name

- Recommended when the list of people to vote for is not comprehensive
- Everyone is on a list of people to vote for is on that list
- Voters can vote for everyone or limited (e.g. a tie-break)
- If a voter adds a "random" name, tellers find and enter the matching person
- There is a high likelihood of spoiled votes if the names entered incorrectly

**Open Voting at:** (datetime picker)

- Example: Aug 28, 2019, 7:20 PM (6 years ago)
- Label: "When should voters be able to access this election and start working on their ballot? Set to 'wait.'"

**Close Voting at:** (datetime picker)

- Example: Aug 28, 2021, 1:42 PM (3 years ago)
- Label: "When should this election be closed? Use your best guess when setting up the election. The Head Teller will adjust the Close time during the election using the Monitor Progress page."

**Other Information Section:**

- Teller Names (with info icon)
- Footer: Glen L.

---

## 2. Elections List Page

**Layout:** Card-based list of elections

**Election Card Components:**

**Header:**

- Election Name (large text)
- Test badge (red pill) or Live status
- Status indicators (right side):
  - "Gathering Ballots" (green badge)
  - "Setting Up" (blue badge)
  - "Processing Ballots" (yellow badge)

**Metadata Display:**

- Last Convenor: [Name] - [Date]
- Election State: [State]
- Tellers: [Count] ([count] ballots entered)
- Online Voting: [Status + additional text]
  - Examples:
    - "Closed - Open: 2023 Apr 29, a select ago"
    - "Closed: 2023 Mar 04, 9:47 AM - 4 years ago"
    - "Submitter 1 of 3 or 1 year ago"
    - "Open Now. Expected to close in 18 days"
- Online ballot: [Status text]
- Front Desk: [Status text]

**Action Buttons (right side):**

- "Enter" (blue, primary action)
- "Tellers" button (with checkmark indicator)
- "Online Voting" button (with X indicator)
- "Other Actions" button

**Expanded "Other Actions" Menu:**

- Edit (link)
- Show Details (link)
- Other options (expandable)

**Bottom Section:**

- "Preparing for a new Election?" heading
- "Start a new election" (blue button)
- "Or, load a previously saved Election File:"
  - "Choose File | No file chosen" (file picker)
- Links:
  - "Key Admin"
  - "Log Out"
  - "Logged in securely with glen.little@gmail.com"
- Footer: © 2026 Glen Little | Version: 3.6.2 | Status & Feedback Document

**Table View (Alternative):**
Admin view shows table with columns:

- Admin | Email | Role | C | WhoLastD | LoadGiving | Action
- Shows multiple rows with Edit links

---

## 3. Dashboard Page (Context-Sensitive)

**Header Section:**

- Election title in quotes with subtitle
- Right side indicators:
  - "Teller" or "Admin" badge
  - "Tellers ✓" (link with checkmark)
  - "Online Voting ✗" (link with X)

**Primary Navigation (Tabs based on state):**

- Election State info icon
- "Setting Up" (or current state)
- "Gathering Ballots"
- "Processing Ballots"
- "Finalized" (with info icon)
- "Other Pages"

**Instructions Section:**

- "Hide Instructions & Tips" link
- Numbered instructions (1-4) in blue box
- Collapsible

**Workflow Sections (Colored Headings):**

1. **"Setting Up - Preparing for the election"** (orange/brown text)

2. **"Gathering Ballots - Registration and Voting"** (blue text)

3. **"Processing Ballots - Tallying and Analyzing"** (green text)
   - Cards with:
     - "Monitor Progress" - Centrally monitor progress of tellers
     - "Enter Ballots" - Type in the names found on ballots
     - "View Reports" - Generate election reports
     - "Display Tie-Breaks" - Show tie-break information (with projector icon)

**Other Pages Section:**

- Simple heading with no action cards shown in screenshot

**Assistant Teller View Differences:**

- Limited button visibility
- Current state determines which buttons shown
- Not all workflow sections visible

**Full Teller/Admin View:**

- All buttons available
- Current state highlighted
- Context help for each section

---

## 4. Front Desk Registration

**Navigation Context:**

- Menu: "Gathering Ballots" → "Front Desk"
- Related pages: "Sort Envelopes", "Roll Call", "Count Envelopes"

**Header Controls (Critical):**

- **Location:** Dropdown (e.g., "Main Location")
  - Shows current voting location
  - Multi-location support
- **Teller at Keyboard:** Dropdown (e.g., "Glen")
  - Required field
  - Shows red warning text if not selected
- **Assisting:** Dropdown (e.g., "Which Teller?")
  - Optional second teller

**Red Warning Example:**
"Red text is gone after changing 'Teller at Keyboard'"

**Instructions:**

- Blue box with numbered instructions
- "Hide Instructions & Tips" link
- Instructions specific to Front Desk workflow

**Search Interface:**

- "Search for a person:" label with count (e.g., "604 people on file")
- Search input box
- Real-time filtering as user types
- "Show All" link

**People List Display:**

- Large, easy-to-read name rows
- Walking person icon for eligible voters
- Click to select person
- Enter key to select highlighted person
- Filtered results show immediately

**Person Detail/Action Panel:**

- Appears when person selected
- Shows person's name
- Action buttons:
  - "In Person" button
  - Other registration method buttons (based on config)
- Log updates when button clicked
- Timestamp recorded automatically

**Activity Log:**

- Shows registration status
- Indicates registration method
- Timestamps
- Teller who processed

---

## 5. Roll Call Page

**Purpose:** High-speed check-in for large groups

**Display Options Panel:**

- "Highlight:" dropdown (e.g., "all voting methods \*")
- "Show ballot delivery methods \*" checkbox (checked by default)
- "\* are recommended" note

**Instructions Panel:**

- Blue box with keyboard shortcuts
- "To return to these instructions: Press Home or scroll back to the top of this page."
- "To view in full-screen mode: Press F11."
- "To back up to the previous name: Press Up or K."
- "To move quickly through the list: Press Up/Down, PgUp/PgDn."
- "To advance to the next name: Press Space, Enter, J, Down Arrow or Click any name in the list."
- "Start Roll Call Now" button (green)

**Full-Screen Mode:**

- Large text display
- One name per screen
- Walking icon indicator
- Name format: "Last, First"
- Keyboard-only navigation
- Very large font size for visibility
- Clean, minimal interface

**Workflow:**

1. Press "Start Roll Call Now"
2. First person appears full-screen
3. Press Space/Enter to mark present and advance
4. Continue through entire list
5. Press Home to return to instructions

---

## 6. Monitor Progress Page

**Purpose:** Real-time dashboard for head teller

**Top Section - "Ballots needing attention"**

**Table:** Ballot | Status | Tellers | Location

- Example rows:
  - OL5 🔒 | Raw - Teller to Finish | Glen | Online
  - OL6 🔒 | Raw - Teller to Finish | | Online
  - OL8 🔒 | Raw - Teller to Finish | | Online
- Lock icon (🔒) indicates ballot editing status
- Link to ballot on ballot number

**Middle Section - Location Status Table**

**Columns:** Location | % | Ballots (Counted / Entered) | Status | Computers | Contact Info

**Example Rows:**

- Main Location | 50% | 12 | 6 🔒 | - | Code: A, Ballots: 6, Current Tellers: Glen (now)
- Online | 100% | 10 | 10 🔒 | - | Code: OL, Ballots: 10

**Key Features:**

- Progress percentage per location
- Real-time ballot counts
- Computer tracking with codes
- Current teller names with last activity time
- Contact info column (expandable)
- Location status indicator

**Control Panel:**

- "☑ Open this election to allow other tellers to participate." checkbox
  - Shows "Teller access code is: testsetst"
- "☑ Auto-refresh this page every [dropdown: minute]"
  - Status: "Refreshed at 2:53:32 PM (a few seconds ago)"
  - "Refresh Now" button

**Online Voting Section**

**Status Display:**

- Large status badge (e.g., "Closed 3 years ago")
- Datetime display: "2023-04-29 13:42:42"
- Label: "(closing day and time)"
- Action button: "Open for 5 minutes"

**Processing:**

- "0 ballots ready to process."
- "Process Online Ballots that are Submitted" button (blue, large)

**Voters Table**

**Columns:** Voter | Email | Phone | Online Ballot | Front Desk Registration | When

**Status Colors:**

- Green = Processed
- Red = Submitted
- Gray headers

**Example Data:**

- Housin, Sohrab | | | Processed ☑ | Kiosk ☑ | Online: 2023 Apr 29, 1:42 pm; Front Desk: 2023 Apr 29, 1:34 pm; kiosk code: Kiosk
- Smith, Mona | | +14034027106 | Submitted - ☑ | In Person ☑ | Front Desk: 2023 Apr 29, 1:46 pm; In Person: Glen
- Little [Little], Glen [Glen] | glen.little+test3@g... | | Processed ☑ | Online ☑ | Online: 2019 Oct 16, 8:56 pm; Front Desk: 12:56 am; Online

**Key Features:**

- Color-coded status (green/red)
- Checkbox indicators
- Detailed timestamp trails
- Teller names in activity log
- Method tracking (Kiosk, Online, In Person, email)

---

## 7. Ballot Entry Page

**Header Controls (Same as Front Desk):**

- **Location:** Dropdown (e.g., "Main Location") with info icon
- **Teller at Keyboard:** Dropdown (e.g., "Glen") with info icon
- **Assisting:** Dropdown (e.g., "Which Teller?")
- "Hide Instructions & Tips" button

**Instructions Panel:**

- Blue box with 5 numbered instructions
- Collapsible
- Context-specific help

**Location Status Display:**

- "▸ Location Status: Unknown" (collapsible section)
- Updates based on location dropdown selection
- Shows location-specific progress

**Ballots Section:**

- "▸ Ballots" (collapsible section)
- "Ballots to enter:" text input (e.g., "12")
- "Refresh List" button
- "Ballots entered (All): 6" count
- "Show All" dropdown
- "Start New Ballot" button (green, prominent)

**Ballot List:**

- Shows ballot codes (e.g., "A2 - Ok", "A3 - Ok", "A4 - Ok")
- Status indicators:
  - "Ok" (green)
  - "Too Few" (red)
  - Other validation states
- Click to edit existing ballot
- Scroll list of ballots

**Vote Entry Section:**

- "▸ Add votes to Ballot #" (collapsible section)
- Opens when "Start New Ballot" clicked
- Search interface for adding names
- Vote count display
- Auto-save functionality

**Toggle Feature:**

- "Toggle 'Location Status'" link
- Shows/hides location information panel

---

## 8. Ballot Entry - Name Search (Sub-feature)

**When Adding Votes:**

**Search Input:**

- Large text input box
- Real-time search/filtering
- Type-ahead functionality
- "Search" placeholder

**Search Results Display:**

- Dropdown/list appears below input
- Shows matching names
- Format: "Last, First" or full name
- Highlight on hover
- Enter key to select
- Up/Down arrows to navigate

**Selected Votes Display:**

- List of names added to ballot
- Order preserved
- Ability to remove (implied)

**Example from Screenshot:**

- User typed text in search
- Name "mickey mouse" added
- Ready to add "minnie mouse"

---

## 9. Reports Page

**Layout:** Two-column layout

**Left Column - Report Categories & List:**

**Ballot Reports (10 reports):**

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

**Voter Reports (10 reports):**

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

**Action Button:**

- "Print (Ctrl+P)" button (teal/cyan)

**Note:**

- "Some browsers, such as Chrome, can create a PDF copy of the report when printing."

**Right Column - Report Display Area:**

- Large white panel
- Shows "Please select a report..." when none selected
- Displays full report when selected
- Print-ready formatting

**Navigation:**

- Reports clickable in left list
- Blue links
- Numbered for easy reference
- Organized by category

---

## 10. Online Voter Portal

**Landing Page - Elections List:**

**Header:**

- "TallyJ - Bahá’í Election System" title
- "Log out" button (top right)
- "Take time to meditate" button (centered banner)

**Welcome Message:**

- "Welcome! Your email address is **glen.little@gmail.com**. It is found in the 10 elections shown below."
- Instructions: "If an upcoming election is not listed here, it may not exist in TallyJ or your email address is not registered in it. Please contact the head teller of the election for assistance."

**Elections Table:**

**Columns:**

1. **Election** - Name, Date, Convenor, Type, Contact link
2. **Online Voting** - Status (Open Now, Closed X days/years ago)
3. **Your Name and Ballot** - Name(s), Registration status, Submission status

**Status Colors:**

- Green = Open/Processed
- Blue = Registered
- Gray = Closed/Not available

**Example Entries:**

- Sample Election | Open Now - Expected to close in 18 days | "Prepare my Ballot" button (blue)
- Test 123 | Closed 21 days ago | Registered: Online, Processed 22 Feb 2022 4:40 pm
- [New Election] | Closed 2 years ago | Registered: Online, Submitted 3 Apr 2022 8:12 pm

**Bottom Section:**

**Settings Panel:**

- "Settings for glen.little@gmail.com"
- "☐ Send me an email when my ballot is processed." checkbox
- "Send me a sample email now" button

**Activity Log:**

- "Activity" heading
- "For your review and security, here is your recent activity (with the most recent first)."
- "Refresh" button

**Table:** Election | Action | When | Time

- Shows login/logout events
- Timestamps with dates and times
- Recent activity tracking

**Resources Section:**

- "To help you prepare to vote, here are a few online resources about voting in Bahá’í elections:"
- Links to Bahá’í Quotes on Elections
- "The Sanctity and Nature of Bahá’í Elections" link
- "How Bahá’í Voters Should Vote" link

---

## 11. Online Ballot Preparation

**Header:**

- Election Name (e.g., "Sample Election")
- Voter's Name (e.g., "Little, Glen 2")
- "Return" button

**Status Banner:**

- **"Open Now"** (green background)
- "Expected to close in 18 days"
- Prominent display at top

**Two-Column Layout:**

### Left Column: "Add to your Ballot/Pool"

**Purpose:** Add names to pool for ballot

**Form Fields:**

- "Add a new person to your pool for this election." instruction
- **First name:** text input (placeholder: "First")
- **Last name:** text input (placeholder: "Last")
- **Extra identifying information:** text input
  - Help text: "(Optional: Use this if the name may not be enough. But remember, your vote is anonymous so do not reference yourself in this description!)"
- "Add Person to my Ballot" button

**Voting Guidance Quote:**

- Bahá’í quote about selection criteria
- Source citation: "Universal House of Justice (view source)"

### Right Column: "Voting for 5 People"

**Instructions:**

- "Please add 5 people before submitting your ballot."
- "Use the left side of the page to find or enter a name to add..."

**My Ballot Section:**

- Numbered list (1, 2, 3, 4, 5)
- Note: "(The order of names does not matter)"
- Empty slots shown
- Message: "Your pool is empty. Search for people to add!"

**When Complete:**

- Ballot fills with names
- "Submit my ballot" button appears

---

## 12. Teller Access Code Login

**Modal Dialog:** "Assist as a Teller"

**Purpose:**
"Join an election as a teller to assist in registering voters and tallying ballots."

**Step-by-Step Form:**

1. **Select your election:**
   - Dropdown showing election name (e.g., "Test 123 - 1 (Test 1)")
   - "Refresh" button

2. **Type in the tellers' access code:**
   - Text input box (shows masked/empty)

3. **Join the election:**
   - "Join Now" button (blue, primary)

**Dialog Controls:**

- X close button (top right)

**Error Handling:**

- Shows validation message for invalid code
- Requires valid election selection

**Post-Login:**

- Redirects to Dashboard
- Teller has limited permissions based on role
- Can access allowed pages only

---

## 13. People Management

**Navigation:** "Setting Up" → "Edit People's Names"

**Page Header:**

- Instructions panel (collapsible)
- "Hide Instructions & Tips" link

**Instructions:**

1. "To edit the information for a person, use this page."
2. "Search for any person by typing a part of their name in the input box below, then press Enter when their name is highlighted. Use the Up and Down keys to move through the list of matched names."

**Search Interface:**

- "Search for a person:" label
- Text input with search icon
- "604 people on file" count display
- "Show All" link
- "Add New Person" button (green)

**Search Results:**

- Large scrollable list area
- Real-time filtering
- Keyboard navigation (Up/Down, Enter)
- Shows matching names

**Person Detail Form (when selected):**
Not fully shown in screenshot, but would include:

- Name fields
- Eligibility checkboxes
- Contact information
- Save/Cancel buttons

---

## UI Component Patterns

### Information Icons (ℹ)

- Blue circular icon with "i"
- Shows tooltip or help text on hover/click
- Used next to field labels
- Consistent throughout application

### Collapsible Sections

- "▸" arrow indicator (collapsed)
- "▾" arrow indicator (expanded)
- Click heading to toggle
- Used for: Instructions, Location Status, Ballots list, etc.

### Status Indicators

- **Badges:** Rounded pills with background color
  - Red = Test/Warning
  - Green = Active/Success
  - Blue = Info/Setting Up
  - Yellow/Orange = Processing
- **Icons:**
  - ✓ (checkmark) = Enabled/Complete
  - ✗ (X) = Disabled/Incomplete
  - 🔒 (lock) = Editing/In Use
  - 🚶 (walking person) = Eligible voter

### Button Styles

- **Primary Actions:** Blue/teal background, white text
- **Success Actions:** Green background, white text
- **Warning Actions:** Red background, white text
- **Secondary Actions:** White background, colored border
- **Action Size:** Large buttons for primary workflow actions

### Form Patterns

- Label above or to left of input
- Required fields marked with \*
- Help text in gray below input
- Validation messages in red
- Info icons for additional help

### Navigation Patterns

- Breadcrumb-style state indicators
- Tab-based primary navigation
- Dropdown secondary navigation
- Context-sensitive menus

### Color Coding

- **Blue:** Information, help, instructions
- **Green:** Success, active, ready
- **Red:** Error, warning, attention needed
- **Gray:** Disabled, inactive, secondary
- **Orange/Brown:** Configuration, setup phase
- **Teal/Cyan:** Primary actions, buttons

### Responsive Behaviors

- Instructions collapsible to save space
- Full-screen mode for Roll Call and presentations
- Projector icon (📺) for presentation pages
- Auto-refresh for monitoring pages

---

## Key Findings for v4 Implementation

### 1. Multi-Step Wizards > Simple Forms

Election configuration needs a 4-step wizard, not a single form. Each step is complex with multiple sections.

### 2. State-Based UI is Critical

The entire application changes based on election state. v4 needs similar state management for navigation. The UI should make the state obvious.

### 3. Location Context Everywhere

Location dropdown appears on multiple pages (Front Desk, Ballot Entry). It's a core part of the UI, not an optional feature, if turned on in settings.

### 4. Teller Context Matters

"Teller at Keyboard" and "Assisting" dropdowns are required for audit trail and multi-teller workflows.

### 5. Real-Time Monitoring is Essential

Monitor Progress page is the "command center" - needs real-time updates, location tracking, and ballot status.

### 6. Keyboard-First Design

Roll Call and ballot entry emphasize keyboard navigation for speed. Accessibility and efficiency combined.

### 7. Instructions Are Contextual

Every page has specific instructions in a collapsible blue box. Not generic help - page-specific guidance.

### 8. Reports Are Comprehensive

20+ different reports organized by category. Not just "show results" - detailed analysis and audit reports.

### 9. Online Portal is Separate Experience

Voter portal has completely different UI from admin interface. Calm, simple, guided workflow.

### 10. Progress Indicators Everywhere

Counts, percentages, status badges throughout. Users always know where they are in the process.

---

## Implementation Priority for v4

### HIGH Priority UI Patterns

1. Location dropdown on all relevant pages
2. Teller dropdowns on Front Desk and Ballot Entry
3. State-based navigation system
4. Monitor Progress real-time dashboard
5. Election configuration wizard (4 steps)
6. Instructions panels on all pages

### MEDIUM Priority UI Patterns

7. Roll Call full-screen mode
8. Reports categorization and display
9. Online voter portal (separate from admin)
10. Status badges and indicators
11. Collapsible sections

### LOW Priority UI Patterns

12. Keyboard shortcuts documentation
13. Activity log displays
14. Projector mode indicators
15. Auto-refresh controls
16. Enhanced tooltips

---

**Document Status:** ✅ Complete - Ready for Phase C implementation  
**Next Step:** Use this document to enhance missing_features_detailed.md specifications
