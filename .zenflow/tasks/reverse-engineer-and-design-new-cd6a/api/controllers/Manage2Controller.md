# Manage2Controller API Documentation

## Overview
**Purpose**: **LEGACY - COMPLETELY DISABLED**  
**Base Route**: `/Manage2`  
**Status**: ⚠️ **ENTIRE CONTROLLER IS COMMENTED OUT**  
**Original Purpose**: Voter account management (password, phone, 2FA, external logins)

---

## Current Status

**The entire Manage2Controller.cs file is commented out (lines 1-397).**

This controller is **NOT ACTIVE** in the current TallyJ system.

---

## Original Purpose (No Longer Used)

When active, this controller provided voter account management features similar to ASP.NET Identity's ManageController:

### Original Endpoints (All Commented Out)
1. `GET /Manage2/Index` - Account management dashboard
2. `POST /Manage2/RemoveLogin` - Remove external login (Google/Facebook)
3. `GET /Manage2/AddPhoneNumber` - Add phone number page
4. `POST /Manage2/AddPhoneNumber` - Add phone number
5. `POST /Manage2/EnableTwoFactorAuthentication` - Enable 2FA
6. `POST /Manage2/DisableTwoFactorAuthentication` - Disable 2FA
7. `GET /Manage2/VerifyPhoneNumber` - Phone verification page
8. `POST /Manage2/VerifyPhoneNumber` - Verify phone with code
9. `POST /Manage2/RemovePhoneNumber` - Remove phone number
10. `GET /Manage2/ChangePassword` - Change password page
11. `POST /Manage2/ChangePassword` - Change password
12. `GET /Manage2/SetPassword` - Set password page (for external login users)
13. `POST /Manage2/SetPassword` - Set password
14. `GET /Manage2/ManageLogins` - Manage external logins page
15. `GET /Manage2/LinkLoginCallback` - OAuth callback for linking accounts

### Authorization
- Original: `[AllowVoter]` - For authenticated voters only

---

## Why This Controller Is Disabled

### TallyJ's Current Voter Authentication

TallyJ uses **passwordless one-time code authentication** for voters, documented in `security/authentication.md`.

**Current Voter Authentication System (System 3)**:
- Voters do NOT have accounts
- Voters do NOT have passwords
- Voters authenticate with email/SMS one-time codes (6-digit)
- Voter records stored in `OnlineVoter` table (NOT `AspNetUsers`)
- Voters matched to `Person` records by email/phone

**What This Means**:
- Voters cannot "manage account" - they have no account
- Voters cannot "change password" - they have no password
- Voters cannot "add 2FA" - authentication is already one-time codes
- Voters cannot "link external logins" - no login exists

### Why Manage2Controller Existed (Historical)

**Timeline** (educated guess based on code):
1. **Early TallyJ**: May have used traditional voter accounts with passwords
2. **Migration**: System migrated to passwordless authentication for better UX
3. **Legacy Code**: Manage2Controller left in codebase but commented out
4. **Current State**: Passwordless auth is the only system

**Benefits of Passwordless for Voters**:
- No password to remember
- No account registration required
- Faster voting process
- Better for one-time election participation
- Reduces support burden (no "forgot password" requests)

---

## Related Code References

### ViewModels Referenced (Also Likely Unused)
The controller references these ViewModels (may also be commented out or unused):
- `IndexViewModel` - Account dashboard
- `AddPhoneNumberViewModel` - Phone number input
- `VerifyPhoneNumberViewModel` - Phone verification code
- `ChangePasswordViewModel` - Password change form
- `SetPasswordViewModel` - Set password form
- `ManageLoginsViewModel` - External login list

### Services Referenced
- `ApplicationUserManager` - ASP.NET Identity user manager
- `ApplicationSignInManager` - ASP.NET Identity sign-in manager
- `IAuthenticationManager` - OWIN authentication

### ManageMessageId Enum
```csharp
public enum ManageMessageId
{
  AddPhoneSuccess,
  ChangePasswordSuccess,
  SetTwoFactorSuccess,
  SetPasswordSuccess,
  RemoveLoginSuccess,
  RemovePhoneSuccess,
  Error
}
```

---

## Voter Functionality in Current System

Since Manage2Controller is disabled, how do voters interact with the system?

### Current Voter Workflows

**1. Vote Online**:
- Voter clicks "Vote Online" on public page
- Enters email/phone
- Receives one-time code (email or SMS)
- Enters code to authenticate
- Casts ballot
- Logs out (or session expires)

**2. Kiosk Mode** (optional):
- Voter uses computer at voting location
- Same one-time code process
- No persistent login

**3. No Account Management**:
- Voters cannot "manage account" because there's no account
- Voters cannot "view past votes" (privacy by design)
- Voters cannot "update profile" - election organizers import voter list

### Where Voter Logic Lives

**Current voter authentication**: `AccountController` (System 3)
- `POST /Account/RequestVoterCode` - Request one-time code
- `POST /Account/VerifyVoterCode` - Verify code and authenticate
- `GET /Account/VoterLogout` - Log out voter

**Vote casting**: `VoteController` (to be documented in Task 3)
- Ballot display
- Vote submission
- Thank you page

**Related Documentation**:
- `security/authentication.md` - Complete voter authentication details
- `AccountController.md` - Voter authentication endpoints

---

## Migration Recommendations for .NET Core + Vue 3

### Do NOT Port Manage2Controller

**Recommendation**: Do not implement this controller in new system.

**Why?**:
- Passwordless authentication is superior for this use case
- Traditional account management adds complexity without benefit
- Current system works well - no user complaints about lack of accounts

### If Account Management Is Needed in Future

If future requirements demand voter accounts (unlikely), consider:

**Modern Passwordless Approach**:
- Continue with email/SMS one-time codes (current system)
- Use magic links (click link in email to authenticate)
- Consider WebAuthn / FIDO2 for biometric authentication

**Avoid Traditional Passwords**:
- Users forget passwords
- Password reset flows add complexity
- Security risk (weak passwords, credential stuffing)
- Not needed for one-time election participation

**If Accounts Are Required**:
- Use ASP.NET Core Identity
- Implement OAuth 2.0 (Google, Facebook, Microsoft)
- Add passwordless options as primary method
- Passwords as fallback only

---

## Testing Recommendations

### No Tests Needed

Since this controller is completely disabled, no tests are needed.

### If Re-enabling (Not Recommended)

If this controller is ever re-enabled:
- Test all password change flows
- Test 2FA enrollment and verification
- Test external login linking/unlinking
- Test phone number verification with Twilio
- Ensure voter security (prevent account takeover)

---

## Code Cleanup Recommendation

### Consider Removing This File

**Recommendation**: Delete `Manage2Controller.cs` entirely in new system.

**Why?**:
- Entire file is commented out (397 lines of dead code)
- No references to this controller in active code
- Confusing to have disabled code in codebase
- New system is fresh start - don't port unused code

**If Keeping for Historical Reference**:
- Move to `_Archive` folder
- Add comment at top: "LEGACY - Not used since v3.x migration to passwordless auth"
- Document why it was disabled

**Git History**:
- Git preserves history - can always retrieve if needed
- No need to keep commented code "just in case"

---

## Related Documentation

- **Current Voter Auth**: `security/authentication.md` - Passwordless authentication (System 3)
- **Account Controller**: `AccountController.md` - Current voter authentication endpoints
- **Vote Controller**: VoteController.md (to be documented) - Vote casting

---

## Summary

**Manage2Controller is completely disabled** and should not be migrated to new system.

**Key Points**:
- **Status**: Entire controller commented out (397 lines)
- **Original Purpose**: Voter account management (password, 2FA, phone, external logins)
- **Why Disabled**: TallyJ uses passwordless authentication for voters
- **Current System**: Voters authenticate with email/SMS one-time codes (no accounts, no passwords)
- **Migration**: Do NOT port to .NET Core + Vue 3
- **Recommendation**: Delete file entirely in new system

**TallyJ's voter authentication is deliberately passwordless by design** - this is a feature, not a limitation. Manage2Controller represents an abandoned approach that proved unnecessary.
