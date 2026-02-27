# TallyJ 4 API Documentation

## Overview

TallyJ 4 provides a RESTful API for election management and real-time ballot tallying. The API is built with ASP.NET Core and uses SignalR for real-time updates.

## Base URL

```
https://your-domain.com/api
```

## Authentication

The API uses JWT (JSON Web Tokens) for authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Endpoints

### Authentication

#### POST /api/auth/login
Login with email and password.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password"
}
```

**Response:**
```json
{
  "token": "jwt-token-here",
  "user": {
    "id": "user-guid",
    "email": "user@example.com",
    "name": "User Name"
  }
}
```

#### POST /api/auth/register
Register a new user account.

### Elections

#### GET /api/elections
Get all elections for the current user.

#### POST /api/elections
Create a new election.

**Request:**
```json
{
  "name": "Annual Election 2024",
  "description": "Community election",
  "electionType": "General",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z"
}
```

#### GET /api/elections/{id}
Get election details by ID.

#### PUT /api/elections/{id}
Update election details.

#### DELETE /api/elections/{id}
Delete an election.

### People Management

#### GET /api/elections/{electionId}/people
Get all people for an election.

#### POST /api/elections/{electionId}/people/import
Import people from CSV or Excel file.

#### POST /api/elections/{electionId}/people
Add a person manually.

### Ballots

#### GET /api/elections/{electionId}/ballots
Get all ballots for an election.

#### POST /api/elections/{electionId}/ballots
Create ballots for the election.

#### POST /api/elections/{electionId}/ballots/{ballotId}/vote
Submit a vote for a ballot.

### Results

#### GET /api/elections/{electionId}/results
Get current election results.

#### GET /api/elections/{electionId}/results/live
Get real-time results (via SignalR).

#### POST /api/elections/{electionId}/tally
Trigger result calculation.

### Online Voting

#### POST /api/online-voting/requestCode
Request a verification code for online voting. **SMS Pumping Prevention**: Only sends codes to voters registered in an open election.

**Request:**
```json
{
  "electionGuid": "election-guid-here",
  "voterId": "voter@example.com",
  "voterIdType": "E",
  "deliveryMethod": "email"
}
```

**Parameters:**
- `electionGuid` (required): The GUID of the election
- `voterId` (required): Email address, phone number (+15551234567), or kiosk code
- `voterIdType` (required): 'E' (email), 'P' (phone), or 'C' (kiosk code)
- `deliveryMethod` (required): 'email', 'sms', or 'voice'

**Validation:**
- Election must exist and be currently open for online voting
- Voter must be registered in the election (exists in Person table)
- Email format validated for type 'E'
- International phone format validated for type 'P' (E.164 format)

**Response (Success):**
```json
{
  "message": "Verification code sent successfully."
}
```

**Response (Error):**
```json
{
  "error": "You are not registered to vote in this election."
}
```

#### POST /api/online-voting/verifyCode
Verify a voter's verification code and receive authentication token.

**Request:**
```json
{
  "voterId": "voter@example.com",
  "verifyCode": "ABC123"
}
```

**Response:**
```json
{
  "token": "jwt-token-here",
  "voterId": "voter@example.com",
  "voterIdType": "E",
  "expiresAt": "2024-01-02T00:00:00Z"
}
```

**Notes:**
- Verification codes expire after 15 minutes
- Maximum 5 failed attempts before lockout
- JWT token valid for 24 hours

#### POST /api/online-voting/{electionGuid}/googleAuth
Authenticate a voter using Google OAuth (Google One Tap or Sign-In).

**Request:**
```json
{
  "electionGuid": "election-guid-here",
  "credential": "google-jwt-token-from-one-tap"
}
```

**Parameters:**
- `electionGuid` (required): The GUID of the election (must match URL parameter)
- `credential` (required): Google JWT credential from Google One Tap or Sign-In

**Validation:**
- Google JWT token must be valid and not expired
- Google email must be verified
- Election must exist and be currently open for online voting
- Voter's Google email must be registered in the election (exists in Person table)

**Response (Success):**
```json
{
  "token": "jwt-token-here",
  "voterId": "voter@example.com",
  "voterIdType": "E",
  "expiresAt": "2024-01-02T00:00:00Z"
}
```

**Response (Error):**
```json
{
  "error": "You are not registered to vote in this election."
}
```

**Notes:**
- Uses existing Google OAuth infrastructure (Google.Apis.Auth package)
- Only accepts verified Google emails
- JWT token valid for 24 hours (same as code-based auth)
- Provides same level of security as code verification with SMS pumping prevention

#### GET /api/online-voting/{electionGuid}/electionInfo
Get public information about an election for online voting.

#### GET /api/online-voting/{electionGuid}/candidates
Get the list of candidates for an election.

#### POST /api/online-voting/{electionGuid}/submitBallot
Submit an online ballot for an election.

#### GET /api/online-voting/{electionGuid}/{voterId}/voteStatus
Get the voting status for a specific voter in an election.

## Real-time Updates (SignalR)

The API uses SignalR for real-time updates. Connect to the following hubs:

### MainHub
- **ElectionStatusChanged**: Fired when election status changes
- **ElectionUpdated**: Fired when election details are updated

### FrontDeskHub
- **BallotReceived**: Fired when a new ballot is received
- **BallotProcessed**: Fired when a ballot is processed

### AnalyzeHub
- **ResultsUpdated**: Fired when results are recalculated
- **TallyProgress**: Fired during tally calculation with progress updates

## Error Handling

The API returns standard HTTP status codes:

- `200`: Success
- `201`: Created
- `400`: Bad Request
- `401`: Unauthorized
- `403`: Forbidden
- `404`: Not Found
- `500`: Internal Server Error

Error responses include a JSON object:

```json
{
  "message": "Error description",
  "details": "Additional error information"
}
```

## Rate Limiting

API requests are rate limited to prevent abuse. Limits vary by endpoint but generally allow:
- 100 requests per minute for read operations
- 10 requests per minute for write operations

## WebSockets/SignalR

For real-time features, connect to:
```
wss://your-domain.com/hubs
```

## File Uploads

File uploads (for people import) support:
- CSV files
- Excel (.xlsx) files
- Maximum file size: 10MB

## Pagination

List endpoints support pagination:

```
GET /api/elections?page=1&pageSize=20
```

Response includes pagination metadata:

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "total": 150,
    "totalPages": 8
  }
}
```

## Complete API Documentation

For detailed API documentation with all endpoints, request/response schemas, and interactive testing, visit the Swagger UI at:

```
/swagger
```

When running locally, this is available at: http://localhost:5020/swagger