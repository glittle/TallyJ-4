# End-to-End Integration Testing Guide

## Setup Instructions

### 1. Start Backend Server
```bash
cd backend
dotnet run
```
**Expected Output:**
```
Now listening on: http://localhost:5016
Application started. Press Ctrl+C to shut down.
```

### 2. Start Frontend Server  
```bash
cd frontend
npm run dev
```
**Expected Output:**
```
ROLLDOWN-VITE v7.1.14  ready in XXXms
➜  Local:   http://localhost:8095/
```

### 3. Open Application
Navigate to http://localhost:8095 in your browser

---

## Test Scenarios

### ✅ Test 1: User Authentication

#### 1.1 User Registration
1. Click "Register" link
2. Fill in form:
   - Username: `testuser` (unique)
   - Email: `test@example.com`
   - Password: `Test1234!`
   - Confirm Password: `Test1234!`
3. Click "Register"
4. **Expected**: Redirect to login page with success message

#### 1.2 User Login
1. Enter credentials:
   - Username: `testuser`
   - Password: `Test1234!`
2. Click "Login"
3. **Expected**: Redirect to Dashboard

#### 1.3 Protected Routes
1. Open new incognito window
2. Try to access http://localhost:8095/dashboard
3. **Expected**: Redirect to login page

#### 1.4 Logout
1. Click user menu → Logout
2. **Expected**: Redirect to login page, can't access dashboard

---

### ✅ Test 2: Election Management

#### 2.1 View Dashboard
1. Login and view dashboard
2. **Expected**: See statistics (elections count, recent activity)

#### 2.2 Create Election
1. Navigate to Elections → Create Election
2. Fill form:
   - Name: `Test LSA 2026`
   - Number of Winners: `9`
   - Number of Extra: `2`
   - Election Type: `Normal`
   - Election Mode: `Normal`
3. Click "Create"
4. **Expected**: Success message, redirect to election list

#### 2.3 View Elections List
1. Navigate to Elections
2. **Expected**: See created election in table
3. Test pagination if more than 10 elections

#### 2.4 View Election Details
1. Click on election name
2. **Expected**: See election details, tabs for People/Ballots/Results

#### 2.5 Edit Election
1. Click "Edit" button
2. Change name to `Test LSA 2026 - Updated`
3. Click "Save"
4. **Expected**: Changes saved, name updated in list

#### 2.6 Delete Election (Optional)
1. Click "Delete" button
2. **Expected**: Confirmation dialog
3. Confirm deletion
4. **Expected**: Election removed from list

---

### ✅ Test 3: People Management

#### 3.1 Navigate to People
1. From election details, click "People" tab
2. **Expected**: Empty table with "Add Person" button

#### 3.2 Add Person
1. Click "Add Person"
2. Fill form:
   - First Name: `John`
   - Last Name: `Smith`
   - Can Receive Votes: ✓
   - Can Vote: ✓
   - Area: `Area 1`
3. Click "Save"
4. **Expected**: Person appears in table

#### 3.3 Add Multiple People
1. Add 8 more people (vary names)
2. **Expected**: Table shows all 9 people
3. Verify pagination works

#### 3.4 Search People
1. Type "Smith" in search box
2. **Expected**: Filtered results show only matching people

#### 3.5 Edit Person
1. Click "Edit" icon on a person
2. Change first name
3. Click "Save"
4. **Expected**: Changes reflected in table

#### 3.6 Delete Person
1. Click "Delete" icon
2. **Expected**: Confirmation, then person removed

---

### ✅ Test 4: Ballot & Vote Entry

#### 4.1 Navigate to Ballots
1. From election details, click "Ballots" tab
2. **Expected**: Empty table with "Add Ballot" button

#### 4.2 Create Ballot
1. Click "Add Ballot"
2. Fill form:
   - Ballot Code: `BALLOT001`
3. Click "Create & Add Votes"
4. **Expected**: Vote entry dialog opens

#### 4.3 Add Votes
1. In vote entry dialog:
   - Select person for Rank 1
   - Select different person for Rank 2
   - Continue for ranks 3-9
2. Click "Save Votes"
3. **Expected**: Ballot created with vote count shown

#### 4.4 View Ballot Votes
1. Click "View Votes" icon on ballot
2. **Expected**: See all entered votes with ranks

#### 4.5 Create Multiple Ballots
1. Create at least 10 ballots with different vote patterns
2. **Expected**: All ballots listed with vote counts

#### 4.6 Edit Ballot
1. Click "Edit" on a ballot
2. Change votes
3. **Expected**: Votes updated

---

### ✅ Test 5: Tally Calculation

#### 5.1 Navigate to Tally
1. From election details, click "Calculate Tally" button
2. **Expected**: Tally calculation page opens

#### 5.2 Calculate Tally
1. Verify election type is correct
2. Click "Calculate Tally"
3. **Expected**: 
   - Loading indicator
   - Success message
   - Preview statistics shown

#### 5.3 View Statistics
1. Check statistics displayed:
   - Total ballots
   - Total votes
   - Spoiled ballots
   - Valid votes
2. **Expected**: Numbers match entered data

---

### ✅ Test 6: Results Display

#### 6.1 View Full Results
1. Click "View Full Results" or navigate to Results tab
2. **Expected**: Results table with sections

#### 6.2 Verify Sections
1. Check "Elected" section
2. **Expected**: 9 people (NumberOfWinners)
3. Check "Extra" section
4. **Expected**: 2 people (NumberOfExtra)
5. Check "Other" section
6. **Expected**: Remaining candidates

#### 6.3 Verify Vote Counts
1. Check vote counts for each person
2. **Expected**: Accurate totals matching ballots

#### 6.4 Verify Tie Indicators
1. Look for tie indicators (if any)
2. **Expected**: Ties clearly marked

#### 6.5 Verify Rank Order
1. Check that people are ranked correctly within sections
2. **Expected**: Higher vote counts = higher rank

---

### ✅ Test 7: Error Handling

#### 7.1 Form Validation
1. Try to create election without name
2. **Expected**: Validation error shown

#### 7.2 API Error Handling
1. Stop backend server
2. Try to create election
3. **Expected**: User-friendly error message (not crash)

#### 7.3 Invalid Login
1. Enter wrong password
2. **Expected**: Clear error message

---

### ✅ Test 8: UI/UX Checks

#### 8.1 Loading States
1. Watch for loading indicators during API calls
2. **Expected**: Spinners/loading states visible

#### 8.2 Empty States
1. View election with no people
2. **Expected**: Helpful empty state message

#### 8.3 Responsive Design
1. Resize browser window
2. Test on mobile viewport
3. **Expected**: UI adapts to screen size

#### 8.4 Language Switching
1. Click language selector
2. Switch to French
3. **Expected**: UI text changes to French (partial)

---

## Automated API Testing

Run the automated test script:

```bash
powershell -ExecutionPolicy Bypass -File test-e2e.ps1
```

This will test:
- ✅ User registration
- ✅ User login
- ✅ Create election
- ✅ Get elections list
- ✅ Add people
- ✅ Create ballots with votes
- ✅ Calculate tally
- ✅ Get results

**Expected Output**: All 8 tests pass ✓

---

## Results Template

### Test Execution Summary

| Test Area | Status | Notes |
|-----------|--------|-------|
| User Authentication | ⬜ | |
| Election Management | ⬜ | |
| People Management | ⬜ | |
| Ballot & Vote Entry | ⬜ | |
| Tally Calculation | ⬜ | |
| Results Display | ⬜ | |
| Error Handling | ⬜ | |
| UI/UX | ⬜ | |

### Issues Found

1. 
2. 
3. 

### Recommendations

1. 
2. 
3. 

---

## Success Criteria

- [ ] All authentication flows work
- [ ] CRUD operations work for elections
- [ ] CRUD operations work for people
- [ ] Ballot/vote entry is intuitive
- [ ] Tally calculates correctly
- [ ] Results display accurately with sections
- [ ] No console errors during normal operation
- [ ] Loading states visible during async operations
- [ ] Form validation prevents invalid submissions
- [ ] API errors handled gracefully
