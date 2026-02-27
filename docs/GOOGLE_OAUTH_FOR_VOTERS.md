# Google OAuth Implementation for Online Voters

## Overview

This document outlines the requirements and implementation approach for adding Google OAuth as an alternative authentication method for online voters, as part of the SMS pumping prevention strategy.

## Current State

### System Users (Tellers/Admins)
The system currently supports Google OAuth for system users through:
- `AuthController.GoogleLogin()` - OAuth redirect flow with PKCE
- `AuthController.GoogleCallback()` - OAuth callback handler
- `AuthController.GoogleOneTap()` - Google One Tap sign-in with JWT validation

### Online Voters
Currently, online voters can only authenticate via:
- Email verification codes
- SMS verification codes  
- Voice call verification codes

These codes are now restricted to voters who are:
1. Registered in an election (present in the Person table)
2. The election is currently open for online voting (OnlineWhenOpen <= now <= OnlineWhenClose)

## Proposed Implementation

### Requirements

To allow Google login for voters while maintaining SMS pumping prevention:

1. **Election Context Required**: All Google OAuth flows for voters must include the election GUID
2. **Email/Phone Validation**: After successful Google authentication, validate that:
   - The Google account's email OR phone number matches a Person record in the specified election
   - The election is currently open for online voting
3. **JWT Token Generation**: Issue the same online voter JWT token format used for code-based authentication
4. **Security Logging**: Log all Google OAuth attempts for voters, including failures

### Recommended Approach

#### Option 1: Google One Tap (Recommended)

Extend the existing `GoogleOneTap` endpoint or create a new voter-specific endpoint:

```csharp
[HttpPost("onlineVoting/{electionGuid}/googleAuth")]
[AllowAnonymous]
public async Task<IActionResult> GoogleAuthForVoter(Guid electionGuid, [FromBody] GoogleOneTapRequest request)
{
    // 1. Validate Google JWT token
    var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential);
    
    // 2. Check election is open
    var election = await _context.Elections
        .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);
    
    if (!IsElectionOpen(election))
        return BadRequest("Election is not currently open for voting");
    
    // 3. Find person by Google email or phone
    var person = await _context.People
        .FirstOrDefaultAsync(p => 
            p.ElectionGuid == electionGuid && 
            (p.Email == payload.Email || p.Phone == payload.PhoneNumber));
    
    if (person == null)
        return BadRequest("You are not registered to vote in this election");
    
    // 4. Create/update OnlineVoter record
    var onlineVoter = await GetOrCreateOnlineVoter(payload.Email);
    
    // 5. Generate JWT token (same format as code verification)
    var token = GenerateJwtToken(onlineVoter);
    
    return Ok(new OnlineVoterAuthResponse { Token = token, ... });
}
```

**Advantages:**
- No redirect flow needed
- Works in iframes and popups
- Better mobile experience
- Simpler implementation

#### Option 2: OAuth Redirect Flow

Create a voter-specific OAuth flow similar to the system user flow:

```csharp
[HttpGet("onlineVoting/{electionGuid}/google/login")]
[AllowAnonymous]
public IActionResult GoogleLoginForVoter(Guid electionGuid, string returnUrl)
{
    // Store electionGuid in state parameter
    var properties = new AuthenticationProperties
    {
        RedirectUri = Url.Action(nameof(GoogleCallbackForVoter)),
        Items = { ["electionGuid"] = electionGuid.ToString(), ["returnUrl"] = returnUrl }
    };
    
    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}

[HttpGet("onlineVoting/google/callback")]
[AllowAnonymous]
public async Task<IActionResult> GoogleCallbackForVoter()
{
    // 1. Get external authentication result
    var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
    
    // 2. Extract electionGuid from state
    var electionGuid = Guid.Parse(result.Properties.Items["electionGuid"]);
    
    // 3. Validate as in Option 1
    // ...
}
```

**Advantages:**
- More robust for complex scenarios
- Better error handling
- Standard OAuth flow

**Disadvantages:**
- More complex implementation
- Requires redirect handling in frontend
- Potential issues with mobile apps

### Frontend Integration

#### Google One Tap (Option 1)

```typescript
// Load Google Sign-In library
<script src="https://accounts.google.com/gsi/client" async defer></script>

// Initialize Google One Tap
google.accounts.id.initialize({
  client_id: 'YOUR_GOOGLE_CLIENT_ID',
  callback: handleGoogleCallback
});

// Show One Tap prompt
google.accounts.id.prompt();

// Handle callback
async function handleGoogleCallback(response: any) {
  const result = await axios.post(
    `/api/onlineVoting/${electionGuid}/googleAuth`,
    { credential: response.credential }
  );
  
  // Store JWT token and redirect to voting
  localStorage.setItem('voterToken', result.data.token);
  router.push(`/elections/${electionGuid}/vote`);
}
```

### Configuration Requirements

1. **Google Cloud Console Setup**:
   - Create OAuth 2.0 credentials for the application
   - Add authorized redirect URIs for voter authentication
   - Configure OAuth consent screen with appropriate scopes (email, profile)

2. **Application Configuration** (appsettings.json):
   ```json
   {
     "Google": {
       "ClientId": "YOUR_CLIENT_ID",
       "ClientSecret": "YOUR_CLIENT_SECRET"
     }
   }
   ```

3. **Frontend Configuration** (.env):
   ```env
   VITE_GOOGLE_CLIENT_ID=YOUR_CLIENT_ID
   ```

### Security Considerations

1. **State Parameter**: Always validate the state parameter to prevent CSRF attacks
2. **Token Validation**: Validate Google JWT tokens server-side, never trust client-side validation
3. **Audience Validation**: Ensure JWT audience matches your Google Client ID
4. **Expiry Checking**: Verify JWT token hasn't expired
5. **Email Verification**: Only accept verified emails from Google (`email_verified: true`)
6. **Rate Limiting**: Implement rate limiting on the Google auth endpoint
7. **Audit Logging**: Log all Google authentication attempts with:
   - Election GUID
   - Google email/phone
   - Success/failure reason
   - IP address
   - Timestamp

### Testing Considerations

1. **Test Accounts**: Create test Google accounts for different scenarios:
   - Registered voter in open election
   - Voter not in election
   - Voter in closed election
   - Unverified email address

2. **Integration Tests**: Test the complete flow:
   - Successful authentication
   - Election validation failures
   - Voter not found scenarios
   - Token generation and validation

3. **Security Tests**: Verify:
   - Invalid JWT tokens are rejected
   - Expired tokens are rejected
   - Audience mismatch is detected
   - CSRF protection works

### Migration Path

1. **Phase 1**: Implement Google One Tap for voters (Option 1)
2. **Phase 2**: Add UI toggle to let voters choose between code and Google auth
3. **Phase 3**: Collect metrics on adoption and user preference
4. **Phase 4**: Consider making Google auth the primary method if adoption is high

### API Documentation

Add to Swagger/OpenAPI documentation:

```yaml
paths:
  /api/onlineVoting/{electionGuid}/googleAuth:
    post:
      summary: Authenticate voter using Google OAuth
      parameters:
        - name: electionGuid
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                credential:
                  type: string
                  description: Google JWT credential from One Tap
      responses:
        200:
          description: Successfully authenticated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/OnlineVoterAuthResponse'
        400:
          description: Validation error (election closed, voter not found, etc.)
```

## Related Documentation

- [Google Identity Services Documentation](https://developers.google.com/identity/gsi/web/guides/overview)
- [Google One Tap Documentation](https://developers.google.com/identity/gsi/web)
- [JWT Validation with Google Identity Services](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token)

## References

- Current system user Google auth: `backend/Controllers/AuthController.cs`
- Online voting service: `backend/Services/OnlineVotingService.cs`
- Request code validation: `backend/Services/OnlineVotingService.cs:RequestVerificationCodeAsync`
