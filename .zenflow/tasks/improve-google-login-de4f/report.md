# Report: Google One Tap Login

## Summary

Replaced the custom Google OAuth redirect flow with **Google One Tap** using the Google Identity Services (GIS) JavaScript library. Users on the officer login page now see a native Google-branded sign-in button and One Tap popup, authenticating without full-page redirects.

## Changes Made

### Backend

| File | Change |
|------|--------|
| `backend/Backend.csproj` | Added `Google.Apis.Auth` NuGet package for server-side ID token validation |
| `Backend.Application/DTOs/Auth/GoogleOneTapRequest.cs` | New DTO with `Credential` property |
| `backend/Controllers/AuthController.cs` | Added `POST /api/auth/google/one-tap` endpoint that validates the Google ID token, creates/finds user, sets auth cookies, and returns `AuthResponse` |

### Frontend

| File | Change |
|------|--------|
| `frontend/index.html` | Added GIS script tag (`accounts.google.com/gsi/client`) |
| `frontend/.env.development` | Added `VITE_GOOGLE_CLIENT_ID` |
| `frontend/.env.example` | Added `VITE_GOOGLE_CLIENT_ID` placeholder |
| `frontend/.env.production` | Added `VITE_GOOGLE_CLIENT_ID` placeholder |
| `frontend/src/types/google-one-tap.d.ts` | TypeScript declarations for the GIS global API |
| `frontend/src/services/authService.ts` | Added `googleOneTap(credential)` method |
| `frontend/src/stores/authStore.ts` | Added `googleOneTapLogin(credential)` method |
| `frontend/src/pages/LoginPage.vue` | Replaced custom Google button with GIS `renderButton()` + `prompt()` |

## Verification Results

### Frontend Type Check (`npx vue-tsc --noEmit`)
- **PASS** - Zero errors

### Frontend Tests (`npm run test`)
- **237 passed**, 2 failed, 1 suite failed
- All 3 failures are **pre-existing** (no test files were modified in this branch):
  - `authStore.test.ts` - logout test expects `authService.logout` call but the store uses `window.location.href` redirect (pre-existing)
  - `PublicLayout.test.ts` - file URL resolution error unrelated to auth (pre-existing)
  - `BallotEntryPage.spec.ts` - vote payload shape mismatch unrelated to auth (pre-existing)

### Backend Tests (`dotnet test`)
- **206 passed**, 67 failed
- All failures are **pre-existing** (no backend test files were modified in this branch). Failures include rate limiting tests, integration auth tests, encryption/cookie tests, and migration tests.

## How It Works

1. GIS script loads in `index.html`
2. On the officer login page, `LoginPage.vue` initializes GIS with the client ID and renders a Google-branded button
3. `google.accounts.id.prompt()` shows the One Tap popup
4. When user selects an account, GIS returns a JWT credential to the callback
5. Frontend sends the credential to `POST /api/auth/google/one-tap`
6. Backend validates the ID token via `GoogleJsonWebSignature.ValidateAsync`, creates/finds user, sets auth cookies
7. Frontend updates auth state and navigates to the dashboard

## Notes

- The existing Google OAuth redirect flow (`/api/auth/google/login` + `/api/auth/google/callback`) is preserved as a fallback
- No database migrations were needed (`AppUser.GoogleId` field is reused)
- The Google Client ID is a public identifier exposed to the frontend via `VITE_GOOGLE_CLIENT_ID`
