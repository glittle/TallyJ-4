# Backend Resource to JSON Translation Mapping

This document maps the original `.resx` backend resource keys to their new JSON dotted-key notation in the frontend locale files.

## ErrorMessages.resx → auth.json

All error messages from `ErrorMessages.en.resx` and `ErrorMessages.fr.resx` have been migrated to `frontend/src/locales/{locale}/auth.json` under the `auth.errors.*` namespace.

| Old RESX Key | New JSON Key | English Value | French Value | Status |
|--------------|--------------|---------------|--------------|--------|
| `InvalidCredentials` | `auth.errors.invalidCredentials` | Invalid email or password | Courriel ou mot de passe invalide | ✅ Migrated (existed) |
| `EmailRequired` | `auth.errors.emailRequired` | Email is required | Le courriel est requis | ✅ Migrated (existed) |
| `PasswordRequired` | `auth.errors.passwordRequired` | Password is required | Le mot de passe est requis | ✅ Migrated (existed) |
| `UserNotFound` | `auth.errors.userNotFound` | User not found | Utilisateur non trouvé | ✅ Migrated (added) |
| `EmailAlreadyExists` | `auth.errors.emailAlreadyExists` | Email already exists | Le courriel existe déjà | ✅ Migrated (added) |
| `InvalidToken` | `auth.errors.invalidToken` | Invalid or expired token | Jeton invalide ou expiré | ✅ Migrated (added) |
| `TwoFactorRequired` | `auth.errors.twoFactorRequired` | Two-factor authentication code required | Code d'authentification à deux facteurs requis | ✅ Migrated (added) |
| `Invalid2FACode` | `auth.errors.invalid2FACode` | Invalid two-factor authentication code | Code d'authentification à deux facteurs invalide | ✅ Migrated (added) |

## Common.resx

The `Common.en.resx` and `Common.fr.resx` files were empty (contained no `<data>` entries), so no translations needed to be migrated from these files.

## Summary

- **Total translations migrated**: 8 entries (English and French)
- **Files updated**:
  - `frontend/src/locales/en/auth.json`
  - `frontend/src/locales/fr/auth.json`
- **Source files** (to be removed in cleanup step):
  - `backend/Resources/ErrorMessages.en.resx`
  - `backend/Resources/ErrorMessages.fr.resx`
  - `backend/Resources/Common.en.resx`
  - `backend/Resources/Common.fr.resx`

## Notes

- Three translations (`InvalidCredentials`, `EmailRequired`, `PasswordRequired`) already existed in the frontend locale files with identical values
- Five new translations were added (`UserNotFound`, `EmailAlreadyExists`, `InvalidToken`, `TwoFactorRequired`, `Invalid2FACode`)
- All backend error messages are now centralized in the frontend's `auth.json` files under the `auth.errors.*` namespace
- The backend will need to be updated to use dotted-key notation (e.g., `auth.errors.invalidCredentials`) instead of the old RESX keys
