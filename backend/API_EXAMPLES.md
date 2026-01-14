# TallyJ 4 API Examples

## Authentication

First, get an access token:

```bash
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@tallyj.test\",\"password\":\"Admin@123\"}"
```

Save the `accessToken` from the response.

## Elections

### Get all elections
```bash
curl -X GET http://localhost:5000/api/elections \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get single election
```bash
curl -X GET http://localhost:5000/api/elections/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Create election
```bash
curl -X POST http://localhost:5000/api/elections \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Test Election\",\"electionType\":\"LSA\",\"electionMode\":\"I\",\"numberToElect\":9,\"dateOfElection\":\"2024-01-15\",\"tallyStatus\":\"Setup\",\"ownerLoginId\":\"admin@tallyj.test\",\"listForPublic\":false,\"showAsTest\":true}"
```

### Update election
```bash
curl -X PUT http://localhost:5000/api/elections/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d "{\"electionGuid\":\"ELECTION_GUID\",\"name\":\"Updated Name\",...}"
```

### Delete election
```bash
curl -X DELETE http://localhost:5000/api/elections/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## People

### Get people by election
```bash
curl -X GET http://localhost:5000/api/people/election/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get single person
```bash
curl -X GET http://localhost:5000/api/people/PERSON_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Create person
```bash
curl -X POST http://localhost:5000/api/people \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d "{\"electionGuid\":\"ELECTION_GUID\",\"firstName\":\"John\",\"lastName\":\"Doe\",\"canReceiveVotes\":true}"
```

## Ballots

### Get ballots by election
```bash
curl -X GET http://localhost:5000/api/ballots/election/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get ballots by location (get via ballot guid)
```bash
curl -X GET http://localhost:5000/api/ballots/ballot/BALLOT_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get single ballot
```bash
curl -X GET http://localhost:5000/api/ballots/BALLOT_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Votes

### Get votes by ballot
```bash
curl -X GET http://localhost:5000/api/votes/ballot/BALLOT_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get votes by election
```bash
curl -X GET http://localhost:5000/api/votes/election/ELECTION_GUID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Get single vote (by RowId)
```bash
curl -X GET http://localhost:5000/api/votes/123 \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Using Seeded Test Data

From the seeded database, you can use these GUIDs:

**Election 1**: Springfield LSA 2024
- Look up the GUID by querying `/api/elections`

**Election 2**: National Convention 2024
- Look up the GUID by querying `/api/elections`

Example workflow:
1. Login to get token
2. Get all elections
3. Copy an election GUID
4. Get people for that election
5. Get ballots for that election
6. Get votes for a specific ballot
