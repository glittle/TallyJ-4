# Full SDD workflow

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Workflow Steps

### [x] Step: Requirements
<!-- chat-id: 635f4ee9-43f6-4734-8772-078e51344fde -->

Create a Product Requirements Document (PRD) based on the feature description.

1. Review existing codebase to understand current architecture and patterns
2. Analyze the feature definition and identify unclear aspects
3. Ask the user for clarifications on aspects that significantly impact scope or user experience
4. Make reasonable decisions for minor details based on context and conventions
5. If user can't clarify, make a decision, state the assumption, and continue

Save the PRD to `{@artifacts_path}/requirements.md`.

### [x] Step: Technical Specification
<!-- chat-id: 51065e7b-2dbc-487a-baef-6062996fb880 -->

Create a technical specification based on the PRD in `{@artifacts_path}/requirements.md`.

1. Review existing codebase architecture and identify reusable components
2. Define the implementation approach

Save to `{@artifacts_path}/spec.md` with:
- Technical context (language, dependencies)
- Implementation approach referencing existing code patterns
- Source code structure changes
- Data model / API / interface changes
- Delivery phases (incremental, testable milestones)
- Verification approach using project lint/test commands

### [x] Step: Planning
<!-- chat-id: d9d67164-76ab-4af3-86ad-a403f4510fce -->

Create a detailed implementation plan based on `{@artifacts_path}/spec.md`.

1. Break down the work into concrete tasks
2. Each task should reference relevant contracts and include verification steps
3. Replace the Implementation step below with the planned tasks

Rule of thumb for step size: each step should represent a coherent unit of work (e.g., implement a component, add an API endpoint). Avoid steps that are too granular (single function) or too broad (entire feature).

Important: unit tests must be part of each implementation task, not separate tasks. Each task should implement the code and its tests together, if relevant.

If the feature is trivial and doesn't warrant full specification, update this workflow to remove unnecessary steps and explain the reasoning to the user.

Save to `{@artifacts_path}/plan.md`.

### [x] Step: Implement Secure Token Storage (Cookies)
<!-- chat-id: b6638667-13e4-4011-9614-40e0e1b0a6d3 -->
Implement cookie-based token storage to replace vulnerable localStorage.

- Create `SecureCookieMiddleware.cs` in `backend/TallyJ4.Backend/Middleware/` for cookie handling
- Modify `AuthController.cs` login endpoint to set httpOnly secure cookies instead of returning tokens in response
- Create `secureTokenService.ts` in `frontend/src/services/` for cookie management
- Update `authStore.ts` to use cookies instead of localStorage
- Update `authService.ts` to handle cookie-based authentication
- Add unit tests for cookie middleware and secure token service
- Add integration tests for cookie-based auth flow

**Verification Steps:**
- Confirm tokens are stored in httpOnly secure cookies, not localStorage
- Test that XSS cannot access authentication tokens
- Verify automatic token refresh works with cookies
- Run `dotnet test` and `npm run test` to ensure tests pass

### [x] Step: Fix OAuth Callback Security
<!-- chat-id: 251cd6ec-5957-434c-8f80-e47858b5a880 -->
Remove tokens from OAuth callback URLs and implement secure token exchange.

- Modify `AuthController.cs` GoogleCallback method to remove tokens from redirect URL
- Implement PKCE (Proof Key for Code Exchange) for OAuth flow
- Update callback to store tokens in secure cookies
- Add unit tests for OAuth callback endpoint
- Add integration tests for complete OAuth flow

**Verification Steps:**
- Confirm callback URL contains no tokens or sensitive data
- Test OAuth login flow completes successfully
- Verify PKCE prevents authorization code interception
- Run `dotnet test` for backend tests

### [x] Step: Implement Proper 2FA Secret Encryption
<!-- chat-id: 577a39b6-07a0-4ea1-a015-f7bba844b903 -->
Replace Base64 encoding with AES-GCM encryption for 2FA secrets.

- Create `EncryptionService.cs` in `backend/TallyJ4.Application/Services/Auth/` with AES-GCM implementation
- Modify `TwoFactorService.cs` to use `EncryptionService` instead of Base64
- Add encryption key configuration to `appsettings.json`
- Create EF migration for any schema changes if needed
- Add unit tests for encryption service and 2FA operations

**Verification Steps:**
- Confirm 2FA secrets are properly encrypted in database
- Test 2FA setup and verification still works
- Verify encryption keys are securely configured
- Run `dotnet test` for security component tests

### [x] Step: Add Rate Limiting Middleware
<!-- chat-id: ae29c5ad-2b67-4f44-8dbd-fbc7282ac2db -->
Implement rate limiting to prevent brute force attacks on authentication endpoints.

- Add `Microsoft.AspNetCore.RateLimiting` package to backend project
- Configure rate limiting middleware in `Program.cs`
- Apply rate limiting attributes to login, register, and 2FA endpoints (5/min for login, 3/hour for register)
- Add unit tests for rate limiting behavior
- Add integration tests for rate limited endpoints

**Verification Steps:**
- Test that login endpoint is limited to 5 attempts per minute
- Verify registration is limited to 3 per hour
- Confirm proper error responses for rate limited requests
- Run `dotnet test` for rate limiting tests

### [x] Step: Configure Account Lockout
<!-- chat-id: 414c1bc0-cc96-4d87-ab15-9372485a31a4 -->
Enable and configure ASP.NET Core Identity account lockout to prevent brute force.

- Configure Identity options in `Program.cs` to enable lockout with 5 failed attempts
- Ensure login uses `SignInManager.PasswordSignInAsync` to track failures
- Update login response to indicate lockout status
- Add unit tests for lockout functionality

**Verification Steps:**
- Test that account locks after 5 failed login attempts
- Verify lockout duration is enforced
- Confirm lockout status is communicated to frontend
- Run `dotnet test` for lockout tests

### [x] Step: Reduce JWT Token Expiry
<!-- chat-id: ad9cf9b7-c3e2-42f7-8bf7-bd5e872f895a -->
Shorten JWT access token expiry from 24 hours to 15-30 minutes.

- Modify `JwtTokenService.cs` to use configurable shorter expiry (default 15 minutes)
- Update `appsettings.json` with new JWT expiry settings
- Ensure refresh tokens remain longer-lived
- Add unit tests for token expiry validation

**Verification Steps:**
- Confirm access tokens expire within 15-30 minutes
- Test token refresh flow works correctly
- Verify no breaking changes to existing API contracts
- Run `dotnet test` for token service tests

### [x] Step: Implement Refresh Token Hashing
<!-- chat-id: 792e5f7d-f7fd-44f6-8317-5ce62d3adde8 -->
Store refresh tokens as SHA-256 hashes instead of plain text.

- Add `TokenHash` field to `RefreshToken` entity
- Create EF migration to add the new field
- Modify `JwtTokenService.cs` to hash refresh tokens before storage
- Update token validation to compare hashes
- Add unit tests for token hashing operations

**Verification Steps:**
- Confirm refresh tokens are hashed in database
- Test token refresh flow with hashed tokens
- Verify database contains only hashed values
- Run `dotnet test` and EF migration tests

### [x] Step: Fix 2FA Password Bypass
<!-- chat-id: 65b0c13b-e915-4183-be8b-931177bab572 -->
Ensure 2FA verification requires password validation.

- Modify `AuthController.cs` Verify2FA endpoint to validate password before 2FA check
- Update endpoint to require both password and 2FA code
- Add rate limiting to 2FA verification (10/minute)
- Add unit tests for secure 2FA verification

**Verification Steps:**
- Confirm 2FA cannot be bypassed without password
- Test complete 2FA flow requires both credentials
- Verify rate limiting on 2FA endpoint
- Run `dotnet test` for 2FA tests

### [ ] Step: Add Frontend URL Configuration
Make frontend URLs configurable instead of hardcoded localhost.

- Add frontend URL configuration to `appsettings.json` for all environments
- Modify `AuthController.cs` GetFrontendUrl method to use configuration
- Update OAuth redirect URLs to use configured values
- Add unit tests for URL configuration

**Verification Steps:**
- Test OAuth redirects work in staging/production environments
- Verify configuration overrides work correctly
- Confirm no hardcoded localhost URLs remain
- Run `dotnet test` for configuration tests

### [ ] Step: Add CSRF Protection to OAuth
Implement state parameter validation for OAuth flows.

- Generate and validate `state` parameter in OAuth authorization requests
- Store state securely server-side or hash it
- Reject OAuth callbacks with invalid/missing state
- Add unit tests for state validation

**Verification Steps:**
- Test OAuth flow rejects invalid state parameters
- Verify state prevents CSRF attacks on OAuth
- Confirm state is properly validated on callback
- Run `dotnet test` for OAuth security tests

### [ ] Step: Implement Refresh Token Cleanup Service
Add background service to periodically clean up expired refresh tokens.

- Create `RefreshTokenCleanupService.cs` as a hosted background service
- Configure cleanup schedule (e.g., daily) in `Program.cs`
- Add database index on `ExpiresAt` and `IsRevoked` fields
- Create EF migration for the index
- Add unit tests for cleanup service

**Verification Steps:**
- Confirm expired tokens are removed from database
- Test cleanup runs on schedule without affecting active tokens
- Verify database performance with index
- Run `dotnet test` for cleanup service tests

### [ ] Step: Enforce Email Verification
Require email verification for local account registration.

- Modify registration endpoint to send verification emails
- Update login to check `EmailConfirmed` flag
- Configure email sending in `Program.cs`
- Add unit tests for email verification flow

**Verification Steps:**
- Confirm new registrations require email verification
- Test login fails for unverified accounts
- Verify email verification process works
- Run `dotnet test` for email verification tests

### [ ] Step: Update Password Policy
Implement stronger password requirements following NIST guidelines.

- Configure Identity password options in `Program.cs` for length, complexity
- Update registration validation to enforce new requirements
- Add client-side password strength validation in frontend
- Add unit tests for password policy validation

**Verification Steps:**
- Confirm passwords meet new requirements (length, character types)
- Test registration rejects weak passwords
- Verify frontend provides password strength feedback
- Run `dotnet test` for password validation tests

### [ ] Step: Add Security Audit Logging
Implement comprehensive logging for security-related events.

- Add security event logging throughout auth components
- Configure structured logging for security events
- Add alerts for suspicious patterns (multiple failures, etc.)
- Add unit tests for logging functionality

**Verification Steps:**
- Confirm security events are logged appropriately
- Test log aggregation and monitoring
- Verify no sensitive data in logs
- Run `dotnet test` for logging tests

### [ ] Step: Unit Tests for Security Components
Write comprehensive unit tests for all new and modified security components.

- Unit tests for `EncryptionService`, `RateLimitService`, `RefreshTokenCleanupService`
- Tests for modified services: `JwtTokenService`, `TwoFactorService`, `AuthController`
- Tests for middleware: `SecureCookieMiddleware`
- Achieve 100% coverage for security-critical code

**Verification Steps:**
- Run `dotnet test --filter "Category=Security"` with 100% pass rate
- Confirm code coverage meets requirements
- Verify tests cover edge cases and error conditions
- Document test results in plan.md

### [ ] Step: Integration Tests for Auth Flows
Create integration tests for complete authentication workflows.

- Test complete login flow with cookies and refresh
- Test OAuth flow end-to-end
- Test 2FA setup and verification
- Test account lockout and rate limiting
- Test registration with email verification

**Verification Steps:**
- Run `dotnet test TallyJ4.Tests --filter "Category=Integration"`
- Confirm all auth flows work correctly
- Verify security measures don't break functionality
- Document integration test results

### [ ] Step: Security Penetration Testing
Perform manual security testing to identify vulnerabilities.

- Test for XSS, CSRF, injection vulnerabilities
- Test token storage security
- Test OAuth flow security
- Test rate limiting effectiveness
- Document findings and fixes

**Verification Steps:**
- Complete security audit checklist from spec.md
- Fix any identified vulnerabilities
- Document penetration test results
- Confirm all critical issues resolved

### [ ] Step: Performance Validation
Validate authentication performance under load.

- Load test authentication endpoints
- Test token refresh performance
- Test database performance with security features
- Ensure no performance degradation

**Verification Steps:**
- Run load tests with `dotnet run -- load-test-auth`
- Confirm performance meets requirements
- Document performance benchmarks
- Verify no bottlenecks introduced
