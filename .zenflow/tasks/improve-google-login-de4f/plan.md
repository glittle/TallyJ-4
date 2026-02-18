# Spec and build

## Configuration
- **Artifacts Path**: {@artifacts_path} → `.zenflow/tasks/{task_id}`

---

## Agent Instructions

Ask the user questions when anything is unclear or needs their input. This includes:
- Ambiguous or incomplete requirements
- Technical decisions that affect architecture or user experience
- Trade-offs that require business context

Do not make assumptions on important decisions — get clarification first.

If you are blocked and need user clarification, mark the current step with `[!]` in plan.md before stopping.

---

## Workflow Steps

### [x] Step: Technical Specification
<!-- chat-id: d7fd79f9-ff0d-4fd0-8fd3-6530cc235d84 -->

Difficulty: **Medium**. Specification saved to `.zenflow/tasks/improve-google-login-de4f/spec.md`.

---

### [x] Step: Backend - Add Google One Tap endpoint
<!-- chat-id: 38c338b7-570b-4597-890d-cf0f5857070d -->

1. Add `Google.Apis.Auth` NuGet package to `backend/Backend.csproj`
2. Create `Backend.Application/DTOs/Auth/GoogleOneTapRequest.cs` DTO
3. Add `POST /api/auth/google/one-tap` endpoint in `AuthController.cs`:
   - Validate Google ID token via `GoogleJsonWebSignature.ValidateAsync`
   - Create/find user (reuse logic from `GoogleCallback`)
   - Generate JWT + refresh token, set auth cookies
   - Return `AuthResponse`
4. Run `dotnet build` and `dotnet test`

---

### [x] Step: Frontend - Integrate Google One Tap
<!-- chat-id: 4894ddeb-08b9-4992-9cba-abe95a9dacd3 -->

1. Add GIS script tag to `frontend/index.html`
2. Add `VITE_GOOGLE_CLIENT_ID` env variable to `.env` / `.env.development`
3. Create `frontend/src/types/google-one-tap.d.ts` TypeScript declarations
4. Add `googleOneTap()` method to `authService.ts`
5. Add `googleOneTapLogin()` method to `authStore.ts`
6. Update `LoginPage.vue`:
   - Replace custom Google button with GIS `renderButton()` + `prompt()`
   - Wire callback to send credential to backend and handle auth state
7. Add any needed i18n keys to `en/auth.json` and `fr/auth.json`
8. Run `npx vue-tsc --noEmit` and `npm run test`

---

### [ ] Step: Final verification and report

1. Run full backend tests (`dotnet test`)
2. Run full frontend type check and tests (`npx vue-tsc --noEmit`, `npm run test`)
3. Write report to `.zenflow/tasks/improve-google-login-de4f/report.md`
