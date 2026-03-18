import json
import os

new_keys = {
  "voting.elections.title": "Your Elections",
  "voting.elections.welcome": "Welcome! Your contact information ({voterId}) was found in the elections shown below.",
  "voting.elections.welcomeHint": "If an upcoming election is not listed here, it may not exist in TallyJ or your information is not registered in it. Please contact the head teller of the election for assistance.",
  "voting.elections.loading": "Loading your elections...",
  "voting.elections.loadError": "Failed to load elections. Please try again.",
  "voting.elections.noElections": "No elections found for your account.",
  "voting.elections.col.election": "Election",
  "voting.elections.col.onlineVoting": "Online Voting",
  "voting.elections.col.yourNameAndBallot": "Your Name and Ballot",
  "voting.elections.status.openNow": "Open Now",
  "voting.elections.status.closed": "Closed",
  "voting.elections.status.closedToday": "Closed today",
  "voting.elections.status.closedYesterday": "Closed yesterday",
  "voting.elections.status.closedDaysAgo": "Closed {days} days ago",
  "voting.elections.status.closedMonthsAgo": "Closed {months} months ago",
  "voting.elections.status.closedYearsAgo": "Closed {years} years ago",
  "voting.elections.status.closingSoon": "Closing very soon",
  "voting.elections.status.closingInMinutes": "Expected to close in {minutes} minutes",
  "voting.elections.status.closingAt": "Expected to close at {time}",
  "voting.elections.status.closingAtWithMinutes": "Expected to close in {hours}h {minutes}m (at {time})",
  "voting.elections.status.estimatedSuffix": " (estimated)",
  "voting.elections.status.open": "Open",
  "voting.elections.status.noOnlineVoting": "No online voting",
  "voting.elections.prepareBallot": "Prepare my Ballot",
  "voting.elections.alreadyVoted": "Ballot already submitted",
  "voting.elections.registered": "Registered:",
  "voting.elections.method.online": "Online",
  "voting.elections.footerNote": "If something looks wrong, please stop using TallyJ and send details to your head teller and/or to global technical support.",
  "voting.elections.logout": "Log Out",
  "voting.ballot.authRequired": "Please authenticate first",
  "voting.ballot.loading": "Loading election information...",
  "voting.ballot.backToElections": "Return",
  "voting.ballot.openNow": "Open Now",
  "voting.ballot.alreadyVoted": "You have already voted in this election.",
  "voting.ballot.notOpen": "Online voting is not currently open for this election.",
  "voting.ballot.duplicateError": "You have selected the same candidate multiple times. Please review your ballot.",
  "voting.ballot.electionNotFound": "Election not found",
  "voting.ballot.submit": "Submit Ballot",
  "voting.ballot.submitSuccess": "Ballot submitted successfully!",
  "voting.ballot.onceWarning": "You can only vote once! Please review your ballot carefully before submitting.",
  "voting.ballot.orderNote": "(The order of names does not matter)",
  "voting.ballot.myBallot": "My Ballot",
  "voting.ballot.votingFor": "Voting for {count} People",
  "voting.ballot.searchPlaceholder": "Search for a candidate",
  "voting.ballot.namePlaceholder": "Type a name",
  "voting.ballot.randomModeHint": "Type the names of the people you wish to vote for. Tellers will match these names to registered voters.",
  "voting.ballot.bothModeHint": "Select from the list below, or type a name not on the list. Tellers will process any names not on the list.",
  "voting.ballot.addToPool": "Add to your Ballot/Pool",
  "voting.ballot.addToPoolDescription": "Add a new person to your pool for this election.",
  "voting.ballot.firstName": "First name",
  "voting.ballot.firstNamePlaceholder": "First",
  "voting.ballot.lastName": "Last name",
  "voting.ballot.lastNamePlaceholder": "Last",
  "voting.ballot.extraInfo": "Extra Identifying information",
  "voting.ballot.extraInfoPlaceholder": "(Optional: Use this if the name may not be enough)",
  "voting.ballot.addPersonToPool": "Add Person to my Ballot",
  "voting.ballot.guideline": '"From among the pool of those whom the elector believes to be qualified to serve, selection should be made with due consideration given to such other factors as age distribution, diversity, and gender." \u2014 Universal House of Justice'
}

locales_dir = r"C:\Users\glenl\.zenflow\worktrees\review-voter-experience-1992\frontend\src\locales"
locales = ['ar', 'es', 'fa', 'fi', 'fr', 'hi', 'ko', 'pt', 'ru', 'sw', 'vi', 'zh']

for locale in locales:
    file_path = os.path.join(locales_dir, locale, 'voting.json')
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    if 'voting.elections.title' in data:
        print(f'Skipped {locale} (already has keys)')
        continue
    
    data.update(new_keys)
    
    with open(file_path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')
    
    print(f'Updated {locale}')

print('Done!')
