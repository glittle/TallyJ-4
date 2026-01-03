# PeopleController API Documentation

## Overview
**Purpose**: Voter/candidate management - retrieve and edit people records  
**Base Route**: `/People`  
**Authorization**: Mixed - `[AllowTellersInActiveElection]` for tellers, `[AllowVoter]` for voters  
**Authentication System**: Uses both Teller (System 2) and Voter (System 3) authentication

## Controller Details

This controller manages Person entities (voters/candidates) within an election. It provides different endpoints for:
- **Tellers**: Full access to all people in the election with vote statistics
- **Voters**: Limited access to eligible candidate names (for online voting ballot selection)

**Key Business Logic Classes**:
- `PersonCacher` - Caches all Person records for performance
- `VoteCacher` - Caches Vote records for performance
- `PeopleModel` - CRUD operations on Person entities

---

## Endpoints

### 1. GET `/People/Index`
**Purpose**: Placeholder index method for routing reference  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: `null`

**Response**:
- Always returns `null`

**Business Logic**:
- None - exists only for routing purposes

**SignalR**: None

---

### 2. GET `/People/GetAll`
**Purpose**: Retrieve all people in the current election with vote statistics  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with people array and last vote ID

**Request Parameters**: None (uses `UserSession.CurrentElection`)

**Response**:
```json
{
  "people": [
    {
      "PersonId": 123,
      "FullName": "John Smith",
      "VoteCount": 45,
      "Area": "District 1",
      "OtherInfo": "Notes",
      "CanReceiveVotes": true,
      "IneligibleReason": null,
      // ... other Person fields from PeopleModel.PersonForList
    }
  ],
  "lastVid": 5678  // Last vote C_RowId for incremental updates
}
```

**Error Response**:
```json
{
  "Error": "Election not selected"
}
```

**Business Logic**:
1. Gets current election from `UserSession.CurrentElection`
2. Checks if single-name election mode
3. Loads all people via `PersonCacher`
4. Loads all votes via `VoteCacher`
5. Transforms each Person to list format via `PeopleModel.PersonForList()`
6. Returns people with vote statistics and last vote ID

**Data Access**:
- `PersonCacher(Db).AllForThisElection` - All people in current election
- `VoteCacher(Db).AllForThisElection` - All votes in current election

**SignalR**: None (client polls this endpoint or receives updates via other hubs)

**Migration Notes**:
- Single-name election check affects data formatting
- Vote count calculation is performed in `PeopleModel.PersonForList()`
- `lastVid` enables incremental updates on client side

---

### 3. GET `/People/GetForVoter`
**Purpose**: Retrieve eligible candidates for online voter ballot selection  
**HTTP Method**: GET  
**Authorization**: `[AllowVoter]` (Voter authentication - System 3)  
**Returns**: JSON with array of eligible candidate names

**Request Parameters**: None (uses `UserSession.CurrentElection`)

**Response**:
```json
{
  "people": [
    {
      "Id": 123,
      "Name": "John Smith",
      "OtherInfo": "Notes",
      "Area": "District 1",
      "sort": 4567  // Random or sequential
    }
  ]
}
```

**Empty Response** (if election configured for random-only selection):
```json
{
  "people": []
}
```

**Business Logic**:
1. Checks `Election.OnlineSelectionProcess`:
   - `List` or `Both`: Return candidate list
   - `Random` (default): Return empty array
2. If `Election.RandomizeVotersList == true`:
   - Assign random sort order to each person
3. Otherwise:
   - Sort alphabetically by `FullName`
   - Assign sequential sort order
4. Filter to only people where `CanReceiveVotes == true`
5. Return Id, Name, OtherInfo, Area, and sort value

**Data Access**:
- `PersonCacher(Db).AllForThisElection` - All people, filtered by `CanReceiveVotes`

**Configuration**:
- `Election.OnlineSelectionProcess` - Controls whether list is shown
- `Election.RandomizeVotersList` - Controls sort order

**SignalR**: None

**Migration Notes**:
- Random sort uses `Random.Next(peopleCount * 100)` for sort value
- Client-side must sort by `sort` field
- Voter can only see names, not vote counts or ineligibility reasons
- Commented code suggests previous versions showed ineligibility reasons

---

### 4. GET `/People/GetDetail`
**Purpose**: Retrieve detailed information for a single person  
**HTTP Method**: GET  
**Authorization**: `[AllowTellersInActiveElection]`  
**Returns**: JSON with person details

**Request Parameters**:
- `id` (int) - Person.C_RowId

**Response**: (Determined by `PeopleModel.DetailsFor()`)
```json
{
  "Person": {
    "C_RowId": 123,
    "FullName": "John Smith",
    "Email": "john@example.com",
    "Phone": "+1234567890",
    "Area": "District 1",
    "CanReceiveVotes": true,
    "CanVote": true,
    "VotingMethod": "O",
    // ... all Person fields
  }
}
```

**Business Logic**:
- Delegates to `PeopleModel.DetailsFor(id)`
- Returns complete Person record details

**Data Access**:
- `PeopleModel` queries Person by C_RowId

**SignalR**: None

**Migration Notes**:
- Used for editing person records in teller interface
- May include related data (location, votes, etc.) - see `PeopleModel` for details

---

## Commented/Unused Code

### GetPeople Method (Commented Out)
```csharp
//public JsonResult GetPeople(string search, bool includeMatches = false, bool forBallot = true)
//{
//  var model = new PeopleSearchModel();
//  return model.Search2(search, includeMatches, forBallot);
//}
```

**Analysis**: Search functionality for people - likely replaced by client-side filtering or different search implementation.

---

## Data Models Referenced

### PersonCacher
- **Purpose**: Performance optimization - caches all Person records for current election
- **Property**: `AllForThisElection` - Returns all Person records for current election
- **Usage**: Avoids repeated database queries

### VoteCacher
- **Purpose**: Performance optimization - caches all Vote records for current election
- **Property**: `AllForThisElection` - Returns all Vote records for current election
- **Usage**: Used to calculate vote counts per person

### PeopleModel
- **Method**: `PersonForList(Person, isSingleNameElection, votes)` - Transforms Person to list format with vote counts
- **Method**: `DetailsFor(int id)` - Returns detailed Person record
- **Method**: `SavePerson(Person)` - Saves/updates Person (called from SetupController)
- **Method**: `DeletePerson(int)` - Deletes Person (called from SetupController)

---

## Authorization Details

### Teller Endpoints
- `GetAll()`, `GetDetail()` - Require `[AllowTellersInActiveElection]`
- Accessible to both authenticated tellers (admins) and guest tellers
- Requires active election in session

### Voter Endpoints
- `GetForVoter()` - Requires `[AllowVoter]`
- Accessible only to authenticated voters (email/SMS/kiosk codes)
- Returns limited data (names only, no vote counts)

---

## Session Dependencies

All endpoints depend on:
- `UserSession.CurrentElection` - Current election GUID
- `UserSession.CurrentElectionGuid` - Current election GUID
- `UserSession.VoterId` (for voter endpoints) - Voter's email/phone/kiosk code

---

## Integration Points

### Used By:
- **Front Desk** - People management interface
- **Edit People's Names** - Teller editing interface
- **Online Voting** - Voter ballot selection (GetForVoter)
- **Ballot Entry** - Candidate name lookup

### Calls To:
- `PersonCacher` - Data access layer
- `VoteCacher` - Data access layer
- `PeopleModel` - Business logic layer

---

## .NET Core Migration Recommendations

### API Design
```csharp
// RESTful API endpoints
GET    /api/elections/{electionId}/people              // GetAll
GET    /api/elections/{electionId}/people/{id}         // GetDetail
GET    /api/elections/{electionId}/people/for-voter    // GetForVoter

// Or for voter-scoped endpoints
GET    /api/voter/elections/{electionId}/candidates    // GetForVoter
```

### Authorization
```csharp
[Authorize(Policy = "TellerInActiveElection")]
public async Task<IActionResult> GetAll(Guid electionId) { }

[Authorize(Policy = "Voter")]
public async Task<IActionResult> GetForVoter(Guid electionId) { }
```

### Response DTOs
```csharp
// Teller response
public class PersonListDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public int VoteCount { get; set; }
    public bool CanReceiveVotes { get; set; }
    // ...
}

// Voter response (limited fields)
public class CandidateDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Area { get; set; }
    public string OtherInfo { get; set; }
}
```

### Caching Strategy
- Use `IMemoryCache` or Redis for PersonCacher/VoteCacher equivalents
- Cache key: `election:{electionId}:people` and `election:{electionId}:votes`
- Invalidate on person/vote updates

### Performance
- Add pagination for GetAll if election has 1000+ people
- Consider GraphQL for flexible client queries (get only needed fields)
- Use EF Core async methods (`ToListAsync`, `SingleOrDefaultAsync`)

---

## Testing Scenarios

1. **Teller GetAll**:
   - With votes in election (verify vote counts)
   - With no votes (verify zero counts)
   - Single-name vs. multi-name election modes
   - Large elections (1000+ people)

2. **Voter GetForVoter**:
   - `OnlineSelectionProcess = List` (return names)
   - `OnlineSelectionProcess = Random` (return empty)
   - `RandomizeVotersList = true` (verify random order)
   - `RandomizeVotersList = false` (verify alphabetical order)
   - Filter by `CanReceiveVotes = true` only

3. **Authorization**:
   - Guest teller can access GetAll
   - Authenticated teller can access GetAll
   - Voter CANNOT access GetAll
   - Voter CAN access GetForVoter
   - Teller CANNOT access GetForVoter

4. **Error Handling**:
   - No election in session → "Election not selected"
   - Invalid person ID in GetDetail → null or error

---

## API Call Examples

### JavaScript (Current System)
```javascript
// Teller - Get all people
$.get('/People/GetAll', function(data) {
  var people = data.people;
  var lastVoteId = data.lastVid;
  // Update UI
});

// Voter - Get candidates
$.get('/People/GetForVoter', function(data) {
  var candidates = data.people;
  // Populate ballot selection
});

// Teller - Get person details
$.get('/People/GetDetail', { id: 123 }, function(data) {
  var person = data.Person;
  // Show edit form
});
```

### TypeScript + Axios (Vue 3 Migration)
```typescript
// Teller service
async getAllPeople(electionId: string): Promise<PersonListDto[]> {
  const response = await axios.get(`/api/elections/${electionId}/people`);
  return response.data.people;
}

// Voter service
async getCandidates(electionId: string): Promise<CandidateDto[]> {
  const response = await axios.get(`/api/voter/elections/${electionId}/candidates`);
  return response.data.people;
}

// Teller service
async getPersonDetail(electionId: string, personId: number): Promise<PersonDetailDto> {
  const response = await axios.get(`/api/elections/${electionId}/people/${personId}`);
  return response.data;
}
```

---

## Related Documentation

- **Database**: See `database/entities.md` for Person entity schema
- **Authentication**: See `security/authentication.md` for teller/voter auth details
- **Business Logic**: See `business-logic/overview.md` for PeopleModel details
- **Controllers**: See `SetupController.md` for SavePerson/DeletePerson endpoints
- **UI**: See `ui-screenshots-analysis.md` for Edit People's Names and Front Desk screenshots
