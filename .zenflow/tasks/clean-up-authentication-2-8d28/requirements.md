# Product Requirements Document: Clean Up Authentication

## Overview
This PRD outlines the requirements for addressing critical and important security vulnerabilities in the TallyJ-4 authentication system. The goal is to enhance security, prevent common attack vectors, and follow security best practices while maintaining usability.

## Current Issues Analysis

### Critical Security Issues

1. **JWT Token Storage Vulnerability**
   - **Issue**: JWT tokens are stored in browser localStorage, vulnerable to XSS attacks
   - **Risk**: Any injected script can steal tokens and impersonate users
   - **Impact**: Complete account compromise for affected users

2. **Insecure Google OAuth Callback**
   - **Issue**: Tokens passed in URL query parameters (`?token=...&refreshToken=...`)
   - **Risk**: Tokens appear in browser history, server logs, and referrer headers
   - **Impact**: Token leakage and potential account compromise

3. **Weak 2FA Secret Encryption**
   - **Issue**: TOTP secrets encrypted using Base64 encoding (not encryption)
   - **Risk**: Anyone with database access can read all 2FA secrets
   - **Impact**: Complete bypass of two-factor authentication

4. **Missing Rate Limiting**
   - **Issue**: No rate limiting on authentication endpoints
   - **Risk**: Brute force attacks on passwords and 2FA codes
   - **Impact**: Account compromise through automated attacks

5. **No Account Lockout**
   - **Issue**: Identity lockout options not configured
   - **Risk**: Unlimited failed login attempts
   - **Impact**: Easier brute force success

### Important Security Issues

6. **Excessive JWT Expiry**
   - **Issue**: Access tokens valid for 24 hours
   - **Risk**: Stolen tokens remain valid for too long
   - **Impact**: Extended window for token misuse

7. **Refresh Token Accumulation**
   - **Issue**: Old revoked tokens not cleaned up
   - **Risk**: Database bloat and potential token reuse
   - **Impact**: Performance degradation and security risk

8. **2FA Verification Bypass**
   - **Issue**: Verify2FA endpoint accepts empty password
   - **Risk**: Password authentication can be bypassed
   - **Impact**: Weakened authentication security

9. **Hardcoded Frontend URLs**
   - **Issue**: GetFrontendUrl hardcodes localhost URLs
   - **Risk**: Breaks in production environments
   - **Impact**: Authentication failures in production

10. **Missing CSRF Protection**
    - **Issue**: No state parameter validation in OAuth flow
    - **Risk**: CSRF attacks on OAuth authentication
    - **Impact**: Unauthorized account linking

### Minor Security Issues

11. **Plain Text Refresh Tokens**
    - **Issue**: Refresh tokens stored in plain text
    - **Risk**: Database compromise exposes all refresh tokens
    - **Impact**: Mass account compromise

12. **No Email Verification Enforcement**
    - **Issue**: EmailConfirmed not enforced for local accounts
    - **Risk**: Unverified email accounts
    - **Impact**: Reduced account security

13. **Weak Password Policy**
    - **Issue**: Minimal password requirements
    - **Risk**: Weak passwords easily guessed or cracked
    - **Impact**: Account compromise through weak credentials

## Functional Requirements

### Authentication Security Enhancements

**REQ-SEC-001: Secure Token Storage**
- Replace localStorage with httpOnly secure cookies for JWT tokens
- Implement automatic token refresh using refresh tokens in httpOnly cookies
- Ensure cookies are configured with Secure, HttpOnly, and SameSite flags
- Maintain backward compatibility for existing authentication flows

**REQ-SEC-002: OAuth Security Improvements**
- Implement authorization code flow for Google OAuth
- Remove tokens from URL parameters
- Use server-side token exchange
- Add CSRF protection with state parameter validation
- Implement PKCE (Proof Key for Code Exchange) for additional security

**REQ-SEC-003: 2FA Encryption**
- Replace Base64 encoding with AES-GCM encryption for TOTP secrets
- Use cryptographically secure keys from configuration/key vault
- Ensure encrypted secrets are properly decrypted for TOTP verification
- Maintain QR code generation functionality

**REQ-SEC-004: Rate Limiting**
- Implement rate limiting on all authentication endpoints:
  - Login attempts: 5 per minute per IP
  - Password reset requests: 3 per hour per IP
  - 2FA verification: 10 per minute per IP
- Use ASP.NET Core rate limiting middleware
- Return appropriate HTTP status codes (429 Too Many Requests)
- Include rate limit headers in responses

**REQ-SEC-005: Account Lockout**
- Configure ASP.NET Core Identity lockout options:
  - Max failed attempts: 5
  - Lockout duration: 15 minutes
  - Lockout window: 10 minutes
- Ensure lockout works with password authentication
- Provide user feedback on lockout status
- Allow administrative unlock

### Token Management

**REQ-SEC-006: Token Expiry Reduction**
- Reduce access token expiry to 15-30 minutes
- Maintain refresh token expiry at 7-30 days
- Implement automatic token refresh in frontend
- Handle token expiry gracefully in API calls

**REQ-SEC-007: Refresh Token Cleanup**
- Implement periodic cleanup of expired/revoked refresh tokens
- Add database maintenance job or scheduled task
- Ensure cleanup doesn't impact performance
- Log cleanup operations for audit

**REQ-SEC-008: Refresh Token Hashing**
- Store SHA-256 hashes of refresh tokens instead of plain text
- Use cryptographically secure salt
- Maintain token validation functionality
- Update token generation and validation logic

### Authentication Flow Fixes

**REQ-SEC-009: 2FA Verification Security**
- Require password verification before 2FA code acceptance
- Fix verify2fa endpoint to validate password
- Ensure 2FA cannot bypass password authentication
- Maintain existing 2FA setup and management flows

**REQ-SEC-010: Frontend URL Configuration**
- Move frontend URLs to configuration
- Support multiple environments (development, staging, production)
- Implement environment-specific URL resolution
- Remove hardcoded localhost URLs

### Additional Security Measures

**REQ-SEC-011: Email Verification Enforcement**
- Enforce email verification for local account registration
- Prevent login for unverified email accounts
- Send verification emails on registration
- Provide email verification resend functionality

**REQ-SEC-012: Password Policy Enhancement**
- Update password requirements:
  - Minimum length: 12 characters
  - Require uppercase letters
  - Require lowercase letters
  - Require digits
  - Require special characters
- Consider implementing NIST password guidelines
- Provide clear password requirement feedback

## Non-Functional Requirements

**PERF-001: Performance Impact**
- Ensure security improvements don't degrade authentication performance
- Rate limiting should not impact legitimate users
- Token operations should remain fast (<100ms)

**COMP-001: Backward Compatibility**
- Maintain API compatibility for existing clients
- Support gradual migration of authentication methods
- Ensure existing user sessions remain valid during transition

**AUDIT-001: Security Audit**
- Log all authentication security events
- Implement audit trail for token operations
- Monitor for suspicious authentication patterns
- Provide security event alerts

## Acceptance Criteria

- All critical security issues resolved
- Authentication flows remain functional
- No breaking changes to existing API contracts
- Security improvements validated through testing
- Performance benchmarks maintained
- Documentation updated for new security measures

## Dependencies

- ASP.NET Core Identity configuration updates
- Frontend cookie handling implementation
- Database schema changes for token hashing
- Configuration management updates
- Testing framework updates for security testing

## Risk Assessment

- **High Risk**: Token storage changes may break existing sessions
- **Medium Risk**: OAuth flow changes may require Google app reconfiguration
- **Low Risk**: Rate limiting and lockout are additive security measures
- **Low Risk**: Password policy changes affect new registrations only

## Success Metrics

- Zero XSS vulnerabilities in authentication
- OAuth tokens no longer exposed in URLs
- 2FA secrets properly encrypted
- Brute force attacks mitigated through rate limiting
- Account lockout prevents unlimited attempts
- Token expiry windows reduced appropriately</content>
</xai:function_call">Write