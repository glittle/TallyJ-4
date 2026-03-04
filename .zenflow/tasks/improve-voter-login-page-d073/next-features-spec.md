# Implementation Specification: Extended Voter Auth & Full Teller 2FA

## Background & Architecture

TallyJ-4 has three distinct login systems:

### 1. Online Voters (`/vote/*` routes)
- Isolated auth system; tokens stored in `localStorage` as `voter_token`
- Voter must have an email OR phone number in the `People` table for an **open election**
- On success, an `OnlineVoter` record is created/updated in the `OnlineVoters` table
- Voter JWT carries claims: `voterId`, `voterIdType`, `voterType = "online"`
- Key service: `backend/Services/OnlineVotingService.cs`
- Key controller: `backend/Controllers/OnlineVotingController.cs`
- Key frontend store: `frontend/src/stores/onlineVotingStore.ts`
- Key frontend page: `frontend/src/pages/voting/VoterAuthPage.vue`

### 2. Assistant Tellers
- Access code only; no user account, no SSO

### 3. Full Tellers (election owners/managers)
- Use `AppUser` (ASP.NET Core Identity), JWT via httpOnly cookies
- Already have 2FA infrastructure: `TwoFactorService` is injected in `AuthController`
- Existing endpoint: `POST /auth/setup2fa`
- Login endpoint already checks `Requires2FA` and defers cookie if 2FA pending

---

## IMPORTANT: Current OTP Delivery Is a Stub

`OnlineVotingService.SendVerificationCodeAsync()` (line ~557) currently does nothing:

```csharp
await Task.Delay(100);
return true;
```

All three OTP delivery methods (email, SMS, voice) need real implementation alongside WhatsApp.

---

## Feature 1: WhatsApp OTP Delivery via GreenAPI

### What it is
An additional delivery option on the Phone tab. User enters their phone number and chooses WhatsApp instead of SMS/voice. A 6-digit OTP is sent as a WhatsApp message. Flow is identical to SMS — it's just a different transport.

### Backend Changes

#### `appsettings.json` — add config section
```json
"GreenApi": {
  "IdInstance": "YOUR_INSTANCE_ID",
  "ApiToken": "YOUR_API_TOKEN",
  "BaseUrl": "https://api.green-api.com"
}
```

#### `OnlineVotingService.SendVerificationCodeAsync` — implement all delivery methods
The method signature: `(string recipient, string method, string code)`
- `method = "email"` → send via SMTP or SendGrid (currently stub — implement)
- `method = "sms"` → send via Twilio or configurable SMS provider (currently stub — implement)
- `method = "voice"` → send via Twilio voice (currently stub — can remain stub for now or implement)
- `method = "whatsapp"` → call GreenAPI (new)

GreenAPI call for WhatsApp:
```
POST https://api.green-api.com/waInstance{IdInstance}/sendMessage/{ApiToken}
Content-Type: application/json

{
  "chatId": "{E164_PHONE_NUMBER}@c.us",
  "message": "Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes."
}
```

Phone number format: strip all non-digits, ensure it starts with country code (no leading `+`). E.g. `+64211234567` → `64211234567@c.us`.

Use `HttpClient` (inject `IHttpClientFactory`). Register a named client `"GreenApi"` in `Program.cs`.

#### `RequestCodeDto` — ensure `DeliveryMethod` accepts `"whatsapp"`
The enum/string validation should allow: `"email"`, `"sms"`, `"voice"`, `"whatsapp"`.

### Frontend Changes

#### `frontend/src/pages/voting/VoterAuthPage.vue` — Phone tab
Add `whatsapp` as a third radio option in `deliveryMethod`:
```typescript
const phoneForm = ref({ phone: '', deliveryMethod: 'sms' as 'sms' | 'voice' | 'whatsapp' });
```

In the template, add a WhatsApp radio button with a WhatsApp-green icon/label. The existing phone tab already has SMS/Voice radios — add a third option.

#### i18n — `frontend/src/locales/en/voting.json`
Add:
```json
"voting.auth.phone.whatsapp": "WhatsApp Message",
"voting.auth.phone.whatsappNote": "A WhatsApp message will be sent to this number"
```

---

## Feature 2: Facebook Login for Online Voters

### How it works
1. Frontend loads Facebook JS SDK, user clicks "Continue with Facebook"
2. Facebook returns a short-lived user access token
3. Frontend sends token to backend
4. Backend calls `https://graph.facebook.com/me?fields=id,email&access_token={token}` to get verified email
5. Backend checks if that email exists in `People` table for an open election
6. If yes: create/update `OnlineVoter`, issue voter JWT
7. Frontend stores token in `localStorage` as `voter_token`, redirects to ballot

### Backend Changes

#### New DTO: `backend/DTOs/OnlineVoting/FacebookAuthForVoterDto.cs`
```csharp
public class FacebookAuthForVoterDto
{
    public string AccessToken { get; set; } = string.Empty;
}
```

#### `IOnlineVotingService` — add method
```csharp
Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)>
    FacebookAuthAsync(FacebookAuthForVoterDto dto);
```

#### `OnlineVotingService` — implement `FacebookAuthAsync`
```
1. POST/GET https://graph.facebook.com/me?fields=id,email&access_token={dto.AccessToken}
2. Extract email from response
3. If no email: return error "Your Facebook account has no verified email address. Please use another login method."
4. Find open elections (same query as OTP flow)
5. Check People table: does anyone have this email in an open election?
6. If yes: upsert OnlineVoter with VoterId=email, VoterIdType="E", AuthMethod="Facebook"
7. Generate voter JWT and return OnlineVoterAuthResponse
```

Use `IHttpClientFactory` / named client `"Facebook"` (base URL `https://graph.facebook.com`).

#### `OnlineVotingController` — new endpoint
```csharp
[HttpPost("auth/voter/facebook")]
[AllowAnonymous]
public async Task<IActionResult> FacebookAuthForVoter([FromBody] FacebookAuthForVoterDto dto)
```

### Frontend Changes

#### Facebook JS SDK loading
Add a `loadFacebookSdk()` helper (similar to `loadGisScript()` for Google) that appends the FB SDK script to `<head>`. Initialize with the app ID from `/api/public/auth-config` (add `facebookAppId` to that endpoint's response).

#### `onlineVotingStore.ts` — add action
```typescript
async function facebookAuth(data: { accessToken: string }) {
  // POST to /auth/voter/facebook
  // store token in localStorage as voter_token
}
```

#### `VoterAuthPage.vue` — new Facebook tab
Add a 5th tab `name="facebook"` with:
- Icon: use a blue Facebook "F" icon (can use inline SVG or an El-Plus icon)
- Description text explaining Facebook login
- A button "Continue with Facebook" that calls `FB.login()`, receives access token, calls store action

#### `VoterAuthPage.vue` — tab label i18n
```json
"voting.auth.tabs.facebook": "Facebook",
"voting.auth.facebook.description": "If you have a Facebook account whose email address matches the one registered for this election, you can sign in with Facebook.",
"voting.auth.facebook.button": "Continue with Facebook"
```

#### Backend `auth-config` endpoint
Add `facebookAppId` to the public auth config response so the frontend can configure the SDK without hardcoding.

---

## Feature 3: Kakao Login for Online Voters (South Korea)

### How it works
1. Frontend loads Kakao JS SDK, user clicks "Kakao로 로그인" (Login with Kakao)
2. Kakao returns an access token
3. Frontend sends token to backend
4. Backend calls Kakao user info API to get email (and/or phone)
5. Backend checks against `People` table for open elections
6. If found: create/update `OnlineVoter`, issue voter JWT

### Developer Setup Required (one-time)
- Register at [developers.kakao.com](https://developers.kakao.com)
- Create an app, note the **JavaScript Key** (for frontend) and **Admin Key** (for backend)
- Enable **Kakao Login** feature in the app settings
- Enable **email** scope and **phone_number** scope in "Additional consent" settings
- Add the TallyJ domain to the allowed origins

### Backend Changes

#### New DTO: `backend/DTOs/OnlineVoting/KakaoAuthForVoterDto.cs`
```csharp
public class KakaoAuthForVoterDto
{
    public string AccessToken { get; set; } = string.Empty;
}
```

#### `IOnlineVotingService` — add method
```csharp
Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)>
    KakaoAuthAsync(KakaoAuthForVoterDto dto);
```

#### `OnlineVotingService` — implement `KakaoAuthAsync`
```
1. GET https://kapi.kakao.com/v2/user/me
   Header: Authorization: Bearer {dto.AccessToken}
2. Response contains kakao_account.email (if consented) and kakao_account.phone_number
3. Try email first (VoterIdType="E"), then phone (VoterIdType="P")
4. Find open elections, check People table
5. If found: upsert OnlineVoter, generate voter JWT
```

Note: Kakao phone numbers come in format `+82 10-XXXX-XXXX` — normalize to E.164 (`+821012345678`) and also strip `+` to match DB format.

#### Config: `appsettings.json`
```json
"KakaoApi": {
  "AdminKey": "YOUR_KAKAO_ADMIN_KEY"
}
```

#### `OnlineVotingController` — new endpoint
```csharp
[HttpPost("auth/voter/kakao")]
[AllowAnonymous]
public async Task<IActionResult> KakaoAuthForVoter([FromBody] KakaoAuthForVoterDto dto)
```

#### Backend `auth-config` endpoint
Add `kakaoJsKey` to the public auth config response.

### Frontend Changes

#### Kakao JS SDK loading
```typescript
const loadKakaoSdk = (): Promise<void> => {
  // append <script src="https://t1.kakaocdn.net/kakao_js_sdk/2.7.2/kakao.min.js"> to head
  // on load: Kakao.init(kakaoJsKey)
}
```

#### `onlineVotingStore.ts` — add action
```typescript
async function kakaoAuth(data: { accessToken: string }) {
  // POST to /auth/voter/kakao
}
```

#### `VoterAuthPage.vue` — new Kakao tab
Add a tab `name="kakao"` with:
- Yellow Kakao brand color (#FEE500) button
- Label: "카카오" (or "Kakao" with 카카오 subtitle)
- Button calls `Kakao.Auth.login({ success: callback })`, extracts `access_token`, calls store action

#### i18n
```json
"voting.auth.tabs.kakao": "Kakao",
"voting.auth.kakao.description": "If you have a Kakao account (KakaoTalk) and your registered email or phone matches, you can sign in with Kakao.",
"voting.auth.kakao.button": "카카오로 로그인 (Login with Kakao)"
```

---

## Feature 4: Full Teller 2FA (Enforcement & Profile UI)

### Current State
- `TwoFactorService` exists and is injected into `AuthController`
- `POST /auth/setup2fa` endpoint exists (sets up TOTP with QR code)
- Login flow already checks `Requires2FA` and withholds cookie if 2FA pending
- **Missing**: UI for 2FA setup, enforcement policy, and profile page

### What Needs to Be Done

#### Backend
1. **Enforce 2FA for full tellers** — add an option in appsettings: `"Auth:Require2FAForFullTellers": true`. When a full teller (role = `"Owner"` or `"Admin"`) logs in and has no 2FA configured, return `Requires2FASetup: true` in the login response so the frontend can direct them to the setup page.
2. **Endpoint to check 2FA status**: `GET /auth/2fa/status` → returns `{ isEnabled: bool, method: "totp"|"email"|null }`
3. **TOTP verification endpoint**: already exists — verify it handles the `Requires2FA` pending flow correctly.

#### Frontend
1. **Profile page 2FA section** — under user profile, show 2FA status with setup/disable button
2. **Setup flow**:
   - Call `POST /auth/setup2fa` → receive QR code (base64 PNG) and secret
   - Display QR code with instructions: "Scan with Google Authenticator, Microsoft Authenticator, or Authy"
   - Ask user to confirm with a TOTP code to verify setup succeeded
3. **Login 2FA challenge page** — if login returns `Requires2FA: true`, show a page to enter the TOTP code before completing login
4. **Enforce on first login** — if `Requires2FASetup: true` is returned, redirect to 2FA setup and block access until configured

#### TOTP App Recommendations (for user-facing documentation)
- Google Authenticator (iOS/Android)
- Microsoft Authenticator (iOS/Android)
- Authy (iOS/Android/Desktop)

---

## Shared Backend Patterns to Follow

### Auth config endpoint
`GET /api/public/auth-config` currently returns `{ googleClientId }`.
Extend to return all IdP client IDs:
```json
{
  "googleClientId": "...",
  "facebookAppId": "...",
  "kakaoJsKey": "..."
}
```

### OnlineVoter upsert pattern (follow existing `GoogleAuthAsync`)
Each new SSO method should:
1. Verify the SSO token with the provider
2. Extract email and/or phone
3. Run the open-election / People-table check
4. Upsert `OnlineVoter` with `VoterId`, `VoterIdType`, `WhenLastLogin`
5. Call `GenerateJwtToken(onlineVoter)` — no changes needed to JWT shape
6. Return `OnlineVoterAuthResponse`

### Frontend SSO pattern (follow existing Google implementation in `VoterAuthPage.vue`)
Each new provider follows:
```typescript
watch(activeTab, async (newTab) => {
  if (newTab === 'provider') {
    await nextTick();
    await initProviderSdk();
  }
});
```

---

## Files to Create/Modify Summary

| File | Change |
|------|--------|
| `backend/Services/OnlineVotingService.cs` | Implement real OTP delivery + new `FacebookAuthAsync`, `KakaoAuthAsync` |
| `backend/Services/IOnlineVotingService.cs` | Add new method signatures |
| `backend/Controllers/OnlineVotingController.cs` | Add `/auth/voter/facebook`, `/auth/voter/kakao` endpoints |
| `backend/DTOs/OnlineVoting/FacebookAuthForVoterDto.cs` | New DTO |
| `backend/DTOs/OnlineVoting/KakaoAuthForVoterDto.cs` | New DTO |
| `backend/Controllers/AuthController.cs` | Extend auth-config endpoint; 2FA enforcement logic |
| `backend/appsettings.json` | Add GreenApi, KakaoApi config sections |
| `frontend/src/pages/voting/VoterAuthPage.vue` | Add WhatsApp option, Facebook tab, Kakao tab |
| `frontend/src/stores/onlineVotingStore.ts` | Add `facebookAuth`, `kakaoAuth` actions |
| `frontend/src/locales/en/voting.json` | New i18n keys |
| `frontend/src/locales/fr/voting.json` | French translations for new keys |
| Profile page (TBD) | 2FA setup UI for full tellers |
