# Sign-in methods

Hosts choose which sign-in methods to offer in ordinary configuration (`appsettings`, `SignIn__Officers__Local`, or `TALLYJ_CONFIG_PATH`). No source edit and no Azure setting is required. IIS and Linux read the same keys.

Officer methods and voter methods are separate. Guest teller entry is not in this list. It stays the election passcode (`POST /api/auth/teller-login`), with the existing per-election lockout.

`GET /api/auth/sign-in-capabilities` is what the login pages use. It is the effective list: flag on, and outside Development and Testing the provider settings below are present. A flag does not invent missing credentials. Turning a method off rejects new sign-in calls. It does not sign anyone out.

## Keys and defaults

| Key | Default | Needs |
| --- | --- | --- |
| `SignIn:Officers:Local` | true | Nothing else. Password signup, login, the authenticator step, and password reset. Reset links use `ClientEnv:frontendUrl`, not the request host. |
| `SignIn:Officers:Google` | true | `ClientEnv:googleClientId` for One Tap. Redirect (`GET /api/auth/google/login`) also needs `GoogleClientSecret`. A client id alone still offers One Tap. |
| `SignIn:Officers:Facebook` | true | `ClientEnv:facebookAppId` |
| `SignIn:Officers:Kakao` | true | `ClientEnv:kakaoApiJsKey` |
| `SignIn:Officers:Telegram` | `ClientEnv:enableTelegramLogin` (false when unset) | `Telegram:BotToken` and `ClientEnv:telegramBotUsername` |
| `SignIn:Voters:Google` | true | `Google:ClientId` |
| `SignIn:Voters:Facebook` | true | `ClientEnv:facebookAppId` |
| `SignIn:Voters:Kakao` | true | `ClientEnv:kakaoApiJsKey` |
| `SignIn:Voters:Telegram` | true | `Telegram:BotToken` and `ClientEnv:telegramBotUsername` |
| `SignIn:Voters:EmailCode` | true | The existing mail sender |
| `SignIn:Voters:PhoneCode` | true | Turns the phone tab off when false, including every transport |
| `SignIn:Voters:Sms` | true | `Twilio:AccountSid`, `Twilio:AuthToken`, `Twilio:FromNumber` |
| `SignIn:Voters:Voice` | true | Same Twilio settings |
| `SignIn:Voters:WhatsApp` | true | `GreenApi:IdInstance` and `GreenApi:ApiToken` |
| `SignIn:Voters:DirectCode` | true | Nothing else |

Placeholder values that start with `<` do not count as configured.

If `SignIn:Officers:Telegram` is omitted, `ClientEnv:enableTelegramLogin` still decides officer Telegram. Set the new key when it should differ.

If every officer method is off, the officer login page says so instead of showing an empty form. Leave `SignIn:Officers:Local` true unless another officer method is configured. Voter methods may all be off; in-person elections do not need one.

## Example

Password for officers, and only email codes for voters:

```json
"SignIn": {
  "Officers": {
    "Local": true,
    "Google": false,
    "Facebook": false,
    "Kakao": false,
    "Telegram": false
  },
  "Voters": {
    "Google": false,
    "Facebook": false,
    "Kakao": false,
    "Telegram": false,
    "EmailCode": true,
    "PhoneCode": false,
    "DirectCode": false
  }
}
```

Environment form: `SignIn__Voters__Sms=false`.
