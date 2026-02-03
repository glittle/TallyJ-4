# TallyJ 4 - Quick Start Guide

Get up and running with TallyJ in 15 minutes!

---

## For Administrators

### 1. Installation (5 minutes)

**Option A: Docker (Recommended)**
```bash
# Clone repository
git clone https://github.com/your-org/tallyj4.git
cd tallyj4

# Configure environment
cp .env.docker.example .env.docker
# Edit .env.docker with your settings

# Start services
docker-compose up -d

# Access the application
# Frontend: http://localhost:8095
# Backend API: http://localhost:5016
```

**Option B: Local Development**
```bash
# Backend
cd backend
dotnet restore
dotnet ef database update
dotnet run

# Frontend (new terminal)
cd frontend
npm install
npm run dev
```

### 2. Initial Setup (3 minutes)

1. **Create Admin Account**
   ```bash
   curl -X POST http://localhost:5016/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "admin@example.com",
       "password": "SecurePassword123!",
       "firstName": "Admin",
       "lastName": "User"
     }'
   ```

2. **Assign Admin Role** (via database or admin panel)

3. **Log In**
   - Navigate to http://localhost:8095
   - Enter credentials

### 3. Test with Sample Data (2 minutes)

1. **Enable Seeding** (Development only)
   - Edit `backend/appsettings.Development.json`
   - Set `"SeedOnStartup": true`
   - Restart backend

2. **Verify Sample Data**
   - 3 users created (admin, teller, voter)
   - 2 sample elections
   - Sample ballots and results

---

## For Election Tellers

### Your First Election (10 minutes)

#### Step 1: Create Election (2 minutes)

1. Log in to TallyJ
2. Click **Elections** → **Create New Election**
3. Fill in:
   - **Name**: "Springfield LSA 2024"
   - **Date**: Select election date
   - **Type**: Local Spiritual Assembly (LSA)
   - **Number to Elect**: 9
   - **Tally Method**: STV (recommended)
4. Click **Save**

#### Step 2: Add Voters (3 minutes)

**Option A: Manual Entry**
1. Go to **People** → **Add New Person**
2. Enter:
   - First Name: Jane
   - Last Name: Smith
   - ✓ Can Vote
   - ✓ Can Receive Votes
3. Click **Save**
4. Repeat for all voters

**Option B: Import CSV (Faster!)**
1. Go to **People** → **Import**
2. Download template
3. Fill in Excel:
   ```csv
   FirstName,LastName,CanVote,CanReceiveVotes
   Jane,Smith,true,true
   John,Doe,true,true
   Sarah,Johnson,true,true
   ```
4. Upload CSV → **Import**

#### Step 3: Enter Ballots (3 minutes)

1. Go to **Ballots** → **New Ballot**
2. Set **Computer Code**: AA (or auto-generated)
3. Search for each name:
   - Type "Jane" → Click "Jane Smith"
   - Type "John" → Click "John Doe"
   - Repeat for 9 votes
4. Click **Save Ballot**
5. Enter more ballots (try entering 5-10 for testing)

#### Step 4: Run Tally (1 minute)

1. Go to **Tally**
2. Review ballot summary (should show number entered)
3. Click **Start Tally**
4. Watch real-time progress bar
5. Tally completes automatically

#### Step 5: View Results (1 minute)

1. Go to **Results**
2. See ranked list:
   - **Elected**: Top 9 candidates
   - **Other**: Remaining candidates
3. Click **Export** → **PDF** to save results

🎉 **Congratulations!** You've completed your first election in TallyJ!

---

## Common Tasks

### Quick Reference

| Task                    | Navigation                           | Time     |
| ----------------------- | ------------------------------------ | -------- |
| Create election         | Elections → Create New Election      | 2 min    |
| Add voter               | People → Add New Person              | 30 sec   |
| Import voters           | People → Import                      | 2 min    |
| Enter ballot            | Ballots → New Ballot                 | 1 min    |
| Run tally               | Tally → Start Tally                  | 30 sec   |
| View results            | Results → Select Election            | 10 sec   |
| Export results          | Results → Export → PDF/Excel/CSV     | 30 sec   |
| Add teller              | Tellers → Add Teller                 | 1 min    |
| Create location         | Locations → Add Location             | 1 min    |

---

## Keyboard Shortcuts

Speed up your workflow with these shortcuts:

- **Ctrl+N**: New ballot
- **Ctrl+S**: Save current form
- **Ctrl+F**: Focus search box
- **Esc**: Close dialog/modal
- **Tab**: Navigate between fields
- **Enter**: Submit form

---

## Tips for Election Day

### Before Voting Starts

✅ **Pre-Election Checklist**
- [ ] Create election in TallyJ
- [ ] Import all eligible voters
- [ ] Create voting locations (if multiple sites)
- [ ] Register computers with codes (AA, AB, AC, etc.)
- [ ] Assign tellers to locations
- [ ] Test ballot entry with sample ballots
- [ ] Brief all tellers on TallyJ use

### During Voting

✅ **Best Practices**
- Enter ballots regularly (don't wait until end)
- Use consistent computer codes
- Flag unclear ballots for head teller review
- Save frequently
- Keep paper ballots organized by computer code

### After Voting

✅ **Post-Vote Tasks**
- [ ] Enter all remaining ballots
- [ ] Review flagged ballots
- [ ] Run tally
- [ ] Verify results match manual count (if done)
- [ ] Export results (PDF for records)
- [ ] Back up database
- [ ] Archive paper ballots

---

## Troubleshooting Quick Fixes

### Problem: Can't find person to vote for

**Solution:**
1. Go to **People**
2. Check if person exists
3. If missing, click **Add New Person**
4. Make sure **Can Receive Votes** is checked

### Problem: Ballot won't save (vote count error)

**Solution:**
- Check election's "Number to Elect" (e.g., 9 for LSA)
- Ensure ballot has exactly that many votes
- No more, no less

### Problem: Tally fails

**Solution:**
1. Go to **Ballots**
2. Filter by status: **Review** or **Spoiled**
3. Resolve all flagged ballots
4. Try tally again

### Problem: Results don't match manual count

**Solution:**
1. Check for duplicate ballots (same ballot code)
2. Verify all ballots are marked **OK** status
3. Re-run tally
4. Compare vote-by-vote if still incorrect

---

## Getting Help

### Documentation
- **User Guide**: [USER_GUIDE.md](USER_GUIDE.md) - Full feature documentation
- **Admin Guide**: [ADMIN_GUIDE.md](ADMIN_GUIDE.md) - System administration
- **Migration Guide**: [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - Upgrading from v3

### Support Channels
- **Email**: support@tallyj.com (if available)
- **GitHub Issues**: Report bugs or request features
- **Community Forum**: Ask questions and share tips

### Training Resources
- **Video Tutorials**: [Link to videos] (if available)
- **Interactive Demo**: Try features without real data
- **Sample Elections**: Pre-loaded examples to explore

---

## Next Steps

Now that you've completed the quick start:

1. **Explore Features**
   - Try the reporting features
   - Test online voting (if enabled)
   - Create custom reports

2. **Configure for Your Organization**
   - Set up voting locations
   - Register computers
   - Import your membership list
   - Customize election types

3. **Train Your Team**
   - Share this guide with tellers
   - Practice with test elections
   - Review troubleshooting section

4. **Plan Your First Real Election**
   - Schedule setup time
   - Coordinate with tellers
   - Prepare backup plans

---

## Cheat Sheet

Print this section for election day reference:

### Essential Steps

```
1. CREATE ELECTION
   Elections → Create New Election → Fill form → Save

2. ADD VOTERS
   People → Import CSV (or Add New Person)

3. ENTER BALLOTS
   Ballots → New Ballot → Search names → Save → Repeat

4. RUN TALLY
   Tally → Start Tally → Wait for completion

5. VIEW RESULTS
   Results → Select Election → Export to PDF
```

### Login Credentials (Test/Development)

| Email              | Password     | Role   |
| ------------------ | ------------ | ------ |
| admin@tallyj.test  | TestPass123! | Admin  |
| teller@tallyj.test | TestPass123! | Teller |

*Change these immediately in production!*

---

**Ready to go? Start your first election now!**

For detailed documentation, see the [User Guide](USER_GUIDE.md).

---

**Last Updated:** February 2, 2026  
**Version:** 4.0.0
