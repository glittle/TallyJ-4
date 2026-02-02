# TallyJ 4 - User Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Dashboard](#dashboard)
3. [Managing Elections](#managing-elections)
4. [Managing People (Voters & Candidates)](#managing-people)
5. [Ballot Entry](#ballot-entry)
6. [Tallying Votes](#tallying-votes)
7. [Viewing Results](#viewing-results)
8. [Teller Management](#teller-management)
9. [Location Management](#location-management)
10. [Reports and Export](#reports-and-export)
11. [Tips and Best Practices](#tips-and-best-practices)

---

## Getting Started

### Logging In

1. Navigate to the TallyJ website
2. Enter your email address and password
3. Click **Login**
4. If you forgot your password, click **Forgot Password** to reset it

### Understanding User Roles

TallyJ has three main user roles:

- **Administrator**: Full access to all features, can manage users and system settings
- **Election Teller**: Can manage elections, enter ballots, and view results
- **Voter**: Can view their election information and submit online ballots (if enabled)

---

## Dashboard

The dashboard is your home screen after logging in. It displays:

### Key Metrics
- **Total Elections**: Number of elections in the system
- **Active Elections**: Elections currently in progress
- **Total Voters**: Number of registered voters across all elections
- **Total Ballots**: Number of ballots entered in the system

### Recent Activity
- Latest elections created
- Recent ballot entries
- System notifications

### Quick Actions
- **Create New Election**: Start a new election
- **View Elections**: See all elections
- **Manage People**: Add or edit voters and candidates

---

## Managing Elections

### Creating a New Election

1. Click **Elections** in the navigation menu
2. Click **Create New Election** button
3. Fill in the election details:
   - **Election Name**: (e.g., "Local Spiritual Assembly 2024")
   - **Election Date**: Date the election will be held
   - **Election Type**: LSA, NSA, Convention, etc.
   - **Number to Elect**: How many people will be elected (e.g., 9 for LSA)
   - **Number Extra**: Additional positions (if any)
   - **Tally Method**: STV (recommended) or Condorcet
   - **Allow Ties**: Whether tie votes are permitted
4. Click **Save**

### Viewing Election Details

1. Go to **Elections**
2. Click on an election name to view details
3. View:
   - Election configuration
   - Number of voters
   - Ballot status
   - Current results (if tally has been run)

### Editing an Election

1. Go to **Elections**
2. Click the **Edit** button next to the election
3. Make your changes
4. Click **Save**

⚠️ **Note**: Some fields cannot be changed after ballots have been entered.

### Deleting an Election

1. Go to **Elections**
2. Click the **Delete** button next to the election
3. Confirm deletion

⚠️ **Warning**: This will permanently delete all associated data (voters, ballots, votes, results).

---

## Managing People

### Adding Voters/Candidates

1. Go to **People**
2. Select the election from the dropdown
3. Click **Add New Person**
4. Fill in the details:
   - **First Name**
   - **Last Name**
   - **Can Vote**: Check if person can submit a ballot
   - **Can Receive Votes**: Check if person can be voted for
   - **Age Group**: Adult, Youth, Child (if applicable)
   - **Location**: Voting location assignment
5. Click **Save**

### Importing People from CSV

1. Go to **People**
2. Click **Import**
3. Download the CSV template
4. Fill in the template with voter/candidate data
5. Upload the completed CSV file
6. Map the columns to the correct fields
7. Review the preview
8. Click **Import**

**CSV Format Example:**
```csv
FirstName,LastName,CanVote,CanReceiveVotes,AgeGroup
Jane,Smith,true,true,Adult
John,Doe,true,true,Adult
```

### Editing a Person

1. Go to **People**
2. Find the person in the list
3. Click the **Edit** button
4. Make your changes
5. Click **Save**

### Searching and Filtering

- Use the **Search** box to find people by name
- Use the **Filter** dropdown to show:
  - All people
  - Voters only
  - Candidates only
  - By location

---

## Ballot Entry

### Recording Paper Ballots

1. Go to **Ballots**
2. Select the election
3. Click **New Ballot**
4. Enter the ballot details:
   - **Computer Code**: Location identifier (e.g., "AA", "AB")
   - **Ballot Number**: Sequential number at that location
5. Enter each vote:
   - Search for the person's name
   - Click to add them to the ballot
   - Repeat for each vote (up to the number to elect)
6. If there are issues with the ballot:
   - Mark as **Spoiled** (ballot error, needs replacement)
   - Mark as **Review** (unclear, needs verification)
   - Add notes explaining the issue
7. Click **Save Ballot**

### Ballot Status Indicators

- **✓ OK**: Ballot is valid and counted
- **⚠ Review**: Ballot needs review by teller
- **✗ Spoiled**: Ballot is invalid
- **📝 InReview**: Ballot is being examined

### Editing a Ballot

1. Go to **Ballots**
2. Find the ballot in the list
3. Click **Edit**
4. Make necessary changes
5. Click **Save**

### Deleting a Ballot

1. Go to **Ballots**
2. Click the **Delete** button next to the ballot
3. Confirm deletion

---

## Tallying Votes

### Running the Tally

1. Go to **Tally**
2. Select the election
3. Review the ballot summary:
   - Total ballots entered
   - Valid ballots
   - Spoiled ballots
   - Ballots under review
4. Click **Start Tally**
5. The system will:
   - Validate all ballots
   - Apply the selected voting method (STV or Condorcet)
   - Calculate results
   - Identify ties (if any)
6. View the real-time progress bar

### Understanding Tally Status

- **Not Started**: Tally has not been run
- **In Progress**: Tally is currently running
- **Completed**: Tally finished successfully
- **Failed**: Tally encountered an error

### Re-running the Tally

If you add or edit ballots after tallying:

1. Go to **Tally**
2. Click **Re-run Tally**
3. Confirm that you want to recalculate results

⚠️ **Note**: Previous results will be replaced.

---

## Viewing Results

### Election Results

1. Go to **Results**
2. Select the election
3. View the results table showing:
   - **Rank**: Position in the election
   - **Name**: Candidate name
   - **Vote Count**: Number of votes received
   - **Section**: Elected, Extra, or Other
   - **Tied**: Whether the candidate is in a tie

### Result Sections

- **Elected**: Candidates who won a seat
- **Extra**: Candidates elected to extra positions (if configured)
- **Other**: Candidates who received votes but were not elected

### Handling Ties

If there are tied candidates:

1. Review the tied candidates (marked with 🔗)
2. Use traditional tie-breaking methods:
   - Re-vote among tied candidates
   - Drawing names from a container
3. Manually adjust the final ranking if needed

### Exporting Results

1. Go to **Results**
2. Click **Export**
3. Choose format:
   - **PDF**: Formatted report
   - **Excel**: Spreadsheet with detailed data
   - **CSV**: Raw data for further analysis

---

## Teller Management

### Assigning Tellers

1. Go to **Tellers**
2. Select the election
3. Click **Add Teller**
4. Fill in:
   - **Name**: Teller's name
   - **Computer Code**: Location code they'll use (e.g., "AA")
   - **Is Head Teller**: Check if this person is the head teller
5. Click **Save**

### Teller Responsibilities

**Head Teller:**
- Oversee the entire election process
- Assign computer codes to tellers
- Review flagged ballots
- Run the tally
- Certify final results

**Regular Teller:**
- Enter ballots at assigned location
- Flag unclear ballots for review
- Assist with voter check-in

---

## Location Management

### Adding Voting Locations

1. Go to **Locations**
2. Click **Add Location**
3. Fill in:
   - **Location Name**: (e.g., "Main Hall", "Community Center")
   - **Contact Person**: Person managing this location
   - **Address**: Physical address
4. Click **Save**

### Registering Computers

1. Go to **Locations**
2. Click on a location name
3. Click **Register Computer**
4. The system will generate a computer code (e.g., "AA", "AB", "AC")
5. Write this code on the physical computer
6. Assign the computer to a teller

**Computer Code Format**: Two letters (AA-ZZ)
- First computer: AA
- Second computer: AB
- Etc.

### Assigning Voters to Locations

1. Go to **People**
2. Edit a person
3. Select their **Voting Location**
4. Click **Save**

This helps with:
- Front desk check-in
- Location-based ballot entry
- Voter roll management

---

## Reports and Export

### Available Reports

1. **Election Summary Report**
   - Overview of election configuration
   - Voter and ballot statistics
   - Final results

2. **Detailed Ballot Report**
   - Every ballot with vote-by-vote breakdown
   - Useful for auditing

3. **Voter Roll Report**
   - List of all registered voters
   - Organized by location
   - Check-in status (if front desk used)

4. **Statistical Analysis Report**
   - Voting patterns
   - Participation rates
   - Historical comparisons

### Generating a Report

1. Go to **Reports**
2. Select the report type
3. Choose the election
4. Set any filters (date range, location, etc.)
5. Click **Generate Report**
6. Choose export format (PDF, Excel, CSV)
7. Download the file

### Chart Visualizations

Available charts:
- **Vote Distribution**: Bar chart showing votes per candidate
- **Participation Rate**: Pie chart of ballots entered vs. registered voters
- **Voting Trends**: Line chart for historical elections
- **Location Breakdown**: Votes by voting location

---

## Tips and Best Practices

### Before the Election

✅ **Do:**
- Create the election well in advance
- Import or enter all voters/candidates
- Assign tellers and locations
- Test the system with sample ballots
- Train all tellers on the system

❌ **Don't:**
- Wait until election day to set up
- Forget to assign computer codes
- Skip the test run

### During the Election

✅ **Do:**
- Enter ballots regularly (don't wait until the end)
- Flag unclear ballots immediately for head teller review
- Use consistent computer codes
- Keep paper ballots organized by computer code
- Save frequently

❌ **Don't:**
- Enter ballots from multiple locations on one computer without changing the code
- Guess at unclear names (flag for review instead)
- Delete ballots without head teller approval

### After the Election

✅ **Do:**
- Run the tally multiple times to verify results
- Export results for records
- Keep paper ballots for audit purposes
- Back up the database
- Generate and archive reports

❌ **Don't:**
- Delete the election immediately
- Forget to export results
- Lose the paper ballots

### Data Entry Tips

**Speed up ballot entry:**
- Learn keyboard shortcuts
- Use the search autocomplete
- Enter ballots in batches by location

**Reduce errors:**
- Double-check each vote before saving
- Have a second person verify unclear ballots
- Use consistent name formats

**Handle common issues:**
- **Person not found**: Check spelling, add them to People first
- **Invalid vote count**: Must match "number to elect"
- **Ballot won't save**: Check for duplicate ballot number

---

## Troubleshooting

### Common Issues

**Issue**: "Cannot find person to vote for"
- **Solution**: Go to People, add the person, mark "Can Receive Votes"

**Issue**: "Ballot has too many/too few votes"
- **Solution**: Check the election's "Number to Elect" setting, ensure ballot matches

**Issue**: "Tally fails to run"
- **Solution**: Check for ballots in "Review" status, resolve all issues first

**Issue**: "Results don't match manual count"
- **Solution**: Re-run tally, verify all ballots entered correctly, check for duplicates

**Issue**: "Cannot delete election"
- **Solution**: You may not have permission, contact administrator

---

## Keyboard Shortcuts

- **Ctrl+N**: New ballot
- **Ctrl+S**: Save current form
- **Ctrl+F**: Focus search box
- **Esc**: Close dialog
- **Tab**: Navigate between fields

---

## Support

For assistance:
- Contact your election administrator
- Check the online help documentation
- Email support (if provided)

---

**Last Updated:** February 2, 2026  
**Version:** 4.0.0
