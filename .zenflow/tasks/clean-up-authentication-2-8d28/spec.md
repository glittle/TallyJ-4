# Technical Specification: Clean Up Authentication

## Overview
This technical specification outlines the implementation approach for addressing critical and important security vulnerabilities in the TallyJ-4 authentication system. The implementation will follow existing architectural patterns and maintain backward compatibility where possible.

## Technical Context

### Technology Stack
- **Backend**: ASP.NET Core Web API (.NET 10), Entity Framework Core, SQL Server
- **Frontend**: Vue 3 + TypeScript, Pinia for state management, Axios for HTTP requests
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **Security**: Symmetric encryption, HMAC-SHA256 for JWT signing
- **Database**: SQL Server with EF Core migrations

### Existing Architecture Patterns
- **Service Layer**: Auth services (JwtTokenService, TwoFactorService, LocalAuthService) in `TallyJ4.Application.Services.Auth`
- **DTO Pattern**: Request/Response DTOs in `TallyJ4.Application.DTOs.Auth`
- **Repository Pattern**: EF Core DbContext with entities in `TallyJ4.Domain.Entities`
- **Configuration**: Appsettings.json with environment-specific overrides
- **Frontend State**: Pinia stores with localStorage persistence
- **Error Handling**: Global exception handlers and API error responses

## Implementation Approach

### Security Enhancements Strategy
1. **Incremental Implementation**: Address critical issues first, then important, then minor
2. **Backward Compatibility**: Maintain existing API contracts where possible
3. **Configuration-Driven**: Use configuration for security settings (keys, URLs, limits)
4. **Testing-First**: Implement security measures with comprehensive test coverage
5. **Audit Trail**: Log all security-related operations for monitoring

### Key Design Decisions
- **Token Storage**: Replace localStorage with httpOnly secure cookies for production-grade security
- **Encryption**: Use AES-GCM for 2FA secrets with keys from Azure Key Vault or secure config
- **Rate Limiting**: Implement ASP.NET Core Rate Limiting middleware with Redis backing store
- **OAuth Flow**: Implement PKCE and server-side token exchange for Google OAuth
- **Password Policy**: Adopt NIST guidelines with configurable requirements

## Source Code Structure Changes

### Backend Changes

#### New Files
```
backend/
├── TallyJ4.Application/Services/Auth/
│   ├── EncryptionService.cs          # AES-GCM encryption for 2FA secrets
│   └── RateLimitService.cs           # Rate limiting logic
├── TallyJ4.Application/Services/
│   └── RefreshTokenCleanupService.cs # Background service for token cleanup
├── TallyJ4.Domain/Entities/
│   └── RefreshTokenHash.cs           # Extension for hashed refresh tokens
└── TallyJ4.Backend/Middleware/
    └── SecureCookieMiddleware.cs     # Cookie-based token handling
```

#### Modified Files
```
backend/Controllers/AuthController.cs
├── Add rate limiting attributes to endpoints
├── Implement secure OAuth callback with PKCE
├── Remove tokens from URL redirects
├── Fix verify2fa to require password validation
├── Add frontend URL configuration

backend/TallyJ4.Application/Services/Auth/TwoFactorService.cs
├── Replace Base64 with AES-GCM encryption
├── Add key management from configuration

backend/TallyJ4.Application/Services/Auth/JwtTokenService.cs
├── Reduce default token expiry to 15-30 minutes
├── Add refresh token hashing with SHA-256

backend/Program.cs
├── Add rate limiting middleware configuration
├── Configure Identity lockout options
├── Add secure cookie policies
├── Update password policy requirements
├── Add email verification enforcement

backend/TallyJ4.Domain/Entities/RefreshToken.cs
├── Add TokenHash field for secure storage
├── Update migration for new field
```

### Frontend Changes

#### New Files
```
frontend/src/
├── services/
│   └── secureTokenService.ts         # Cookie-based token management
├── composables/
│   └── useSecureAuth.ts              # Secure authentication composable
└── stores/
    └── secureAuthStore.ts            # Updated auth store with cookie support
```

#### Modified Files
```
frontend/src/stores/authStore.ts
├── Replace localStorage with secure cookie handling
├── Add automatic token refresh logic
├── Update logout to clear secure cookies

frontend/src/services/authService.ts
├── Update API calls to handle cookie-based auth
├── Add refresh token endpoint integration
├── Implement secure OAuth callback handling
```

### Configuration Changes
```
backend/appsettings.json
├── Add encryption key configuration
├── Add rate limiting settings
├── Add secure frontend URLs
├── Update JWT expiry settings

frontend/.env
├── Add secure cookie domain settings
├── Add token refresh configuration
```

## Data Model Changes

### Database Schema Updates
```sql
-- Add hash field to RefreshToken table
ALTER TABLE RefreshToken ADD TokenHash NVARCHAR(64) NULL;

-- Create index for cleanup operations
CREATE INDEX IX_RefreshToken_ExpiresAt_IsRevoked ON RefreshToken (ExpiresAt, IsRevoked);

-- Add encryption key table (optional, for key rotation)
CREATE TABLE EncryptionKeys (
    Id INT PRIMARY KEY IDENTITY,
    KeyId UNIQUEIDENTIFIER NOT NULL,
    KeyData NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
```

### Entity Framework Migrations
- Migration script for RefreshToken.TokenHash field
- Migration script for EncryptionKeys table (if implemented)
- Update existing seed data to use hashed tokens

## API Changes

### New Endpoints
```
POST /api/auth/refresh-token-v2    # Secure refresh with cookie-based tokens
GET  /api/auth/oauth-config       # Get OAuth configuration for frontend
POST /api/auth/verify-2fa-secure  # 2FA verification with password validation
```

### Modified Endpoints
```
POST /api/auth/login
├── Add rate limiting (5/minute per IP)
├── Return tokens in response body only
├── Add lockout feedback

POST /api/auth/register
├── Enforce email verification
├── Update password requirements
├── Add rate limiting (3/hour per IP)

GET /api/auth/google/callback
├── Remove tokens from URL
├── Use secure cookie storage
├── Implement PKCE validation

POST /api/auth/verify2fa
├── Require password validation
├── Add rate limiting (10/minute per IP)
```

### Response Format Changes
- AuthResponse: Add `secureTokens` flag to indicate cookie usage
- Error responses: Include rate limit headers and retry information
- OAuth callback: Return success/error status instead of redirect with tokens

## Delivery Phases

### Phase 1: Critical Security Fixes (Week 1-2)
**Focus**: Address XSS, OAuth leaks, weak encryption, rate limiting, lockout
- Implement secure token storage (cookies)
- Fix OAuth callback to remove tokens from URL
- Replace Base64 2FA encryption with AES-GCM
- Add rate limiting middleware
- Configure account lockout

**Milestones**:
- All critical issues resolved
- Basic authentication flow working with cookies
- OAuth flow secured
- Rate limiting preventing brute force

### Phase 2: Authentication Flow Improvements (Week 3)
**Focus**: Token management, 2FA security, configuration
- Reduce JWT expiry times
- Implement refresh token hashing
- Fix verify2fa password bypass
- Add frontend URL configuration
- Add CSRF protection to OAuth

**Milestones**:
- Token expiry reduced to 15-30 minutes
- 2FA cannot bypass password authentication
- Configuration-driven frontend URLs
- OAuth state parameter validation

### Phase 3: Additional Security Measures (Week 4)
**Focus**: Cleanup, email verification, password policy
- Implement refresh token cleanup service
- Enforce email verification for local accounts
- Update password policy requirements
- Add security audit logging

**Milestones**:
- No expired tokens accumulating in database
- Email verification required for new accounts
- Strong password requirements enforced
- Comprehensive security event logging

### Phase 4: Testing and Validation (Week 5)
**Focus**: End-to-end testing, security validation
- Unit tests for all security components
- Integration tests for authentication flows
- Security penetration testing
- Performance validation

**Milestones**:
- 100% test coverage for auth components
- All authentication flows tested
- Security audit passed
- Performance benchmarks maintained

## Verification Approach

### Testing Strategy
- **Unit Tests**: Test individual security components (encryption, token generation, validation)
- **Integration Tests**: Test complete authentication flows with mocked external services
- **Security Tests**: Penetration testing for XSS, CSRF, injection vulnerabilities
- **Performance Tests**: Validate authentication performance under load

### Test Commands
```bash
# Backend tests
cd backend && dotnet test --filter "Category=Security"

# Frontend tests  
cd frontend && npm run test:security

# Integration tests
cd backend && dotnet test TallyJ4.Tests --filter "Category=Integration"

# Load testing
cd backend && dotnet run -- load-test-auth
```

### Security Validation Checklist
- [ ] JWT tokens not stored in localStorage
- [ ] OAuth tokens not exposed in URLs
- [ ] 2FA secrets properly encrypted
- [ ] Rate limiting prevents brute force
- [ ] Account lockout configured and working
- [ ] Token expiry within secure limits
- [ ] Refresh tokens hashed in database
- [ ] 2FA requires password validation
- [ ] Frontend URLs configurable
- [ ] CSRF protection implemented
- [ ] Email verification enforced
- [ ] Strong password policy active

### Monitoring and Alerts
- Implement security event logging
- Add alerts for suspicious authentication patterns
- Monitor rate limiting effectiveness
- Track token usage and expiry patterns

## Dependencies and Prerequisites

### External Dependencies
- **Microsoft.AspNetCore.RateLimiting**: For rate limiting middleware
- **System.Security.Cryptography**: For AES-GCM encryption
- **Azure.Security.KeyVault.Secrets** (optional): For key management

### Configuration Prerequisites
- Encryption keys configured in secure storage
- Frontend URLs configured for all environments
- OAuth client credentials properly secured
- Database connection with sufficient permissions

### Migration Requirements
- Database backup before schema changes
- Zero-downtime deployment strategy
- Rollback plan for authentication changes
- User communication for breaking changes

## Risk Mitigation

### High Risk Items
- **Token Storage Changes**: May break existing sessions
  - Mitigation: Implement gradual migration with fallback
- **OAuth Flow Changes**: May require Google app reconfiguration
  - Mitigation: Test thoroughly in staging environment

### Rollback Strategy
- Feature flags for new security measures
- Database migration rollback scripts
- Configuration rollback procedures
- Emergency token regeneration capability

## Success Criteria

- All critical security vulnerabilities resolved
- Authentication flows maintain usability
- No performance degradation
- Comprehensive test coverage
- Security audit compliance
- Zero breaking changes to existing API contracts</content>
</xai:function_call">Write