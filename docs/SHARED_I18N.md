# Shared i18n for a .NET backend + Vue frontend

A reusable pattern from [TallyJ v4](https://github.com/glittle/TallyJ-4): one JSON catalog, used by Vue and ASP.NET Core. No `.resx`, no duplicated string tables.

This is the architecture TallyJ actually ships, plus the few traps you should not copy blindly.

---

## The idea

Keep **one source of truth**: flat JSON files next to the Vue app.

- Vue-i18n loads them (nested at runtime).
- The API reads the **same folder** from disk via `IStringLocalizer`.
- The browser sends `Accept-Language` on every request so server-side strings match the UI.

You are not “sharing resources” by copying files into the C# project. You are pointing the API at the frontend locale directory.

```
frontend/src/locales/
  en/auth.json          ← edit here
  en/common.json
  fr/auth.json          ← translators; not English placeholders
  bundled/              ← generated; gitignored; frontend prod only
backend/appsettings.json
  Localization:ResourcesPath = "../frontend/src/locales"
```

---

## File format: flat dotted JSON

Files are `Dictionary<string, string>`, not nested objects:

```json
{
  "common.appTitle": "TallyJ v4",
  "auth.errors.invalidCredentials": "Invalid email or password",
  "tally.section.elected": "Elected"
}
```

Why flat:

- C# deserializes each file as `Dictionary<string, string>` and looks up `"auth.errors.invalidCredentials"` as-is.
- Vue-i18n wants nested objects, so the frontend runs a `flatToNested()` split-on-`.` before `createI18n`.

If you store nested JSON (`{ "auth": { "errors": { ... } } }`), the C# localizer will not find keys. If you store only nested Vue files, you have two catalogs again.

Group files by area (`auth.json`, `tally.json`, `errors.json`). Merge all `*.json` in a locale folder into one catalog. Duplicate keys: first wins; log a warning.

---

## Two ways to use a key

Pick one per string. Mixing them on the same key is how you get double-translation or untranslated keys in the UI.

### 1. Server looks up the JSON (auth errors, emails, TOTP issuer, report section titles)

```csharp
return (false, _localizer["auth.errors.invalidCredentials"], null);
```

Use this when the **API must emit a human string**: login failure, password-reset email, authenticator issuer name, a heading written into a generated document.

### 2. Server returns the key; Vue translates (most UI)

```csharp
public const string BallotsNeedReview = "elections.stageChangeError.ballotsNeedReview";
```

```ts
t(dto.messageKey, dto.parameters)
```

Use this when the client already has vue-i18n. The API stays language-agnostic. Pass interpolation values as a dictionary (or a small `|count=3` suffix if you must keep a single string).

TallyJ uses (1) for auth and tally section labels, and (2) for election-stage errors, import warnings, and tally progress. (2) is the default for new UI.

---

## Frontend (Vue 3 + vue-i18n)

- `legacy: false`, `fallbackLocale: "en"`, `$t()` / `useI18n().t()` / `<i18n-t>`.
- English loaded eagerly; other locales lazy.
- Preferred language: `localStorage`, else `navigator.languages`, else `en`.
- Every API call sets `Accept-Language` from the current locale.
- Production build **merges** per-locale JSON files into `locales/bundled/{lang}.json` (one chunk per language). Dev loads the per-file sources. Never edit `bundled/`.
- RTL: `:lang(ar), :lang(fa) { direction: rtl }`.

Adding a string:

1. Add it **only** under `locales/en/<area>.json`.
2. Use `$t('area.foo')` or `t('area.foo', { name })`.
3. Do not paste English into `fr/`, `es/`, … as a placeholder. Empty/missing is better: it is obvious what still needs translation.

---

## Backend (ASP.NET Core, no .resx)

A small custom `IStringLocalizer` stack reads `{ResourcesPath}/{culture}/*.json`, merges, and caches.

```json
"Localization": {
  "ResourcesPath": "../frontend/src/locales",
  "DefaultCulture": "en",
  "SupportedCultures": [ "en", "fr", "es" ]
}
```

Wire-up:

- `Configure<JsonLocalizationOptions>(configuration.GetSection("Localization"))`
- `services.AddJsonLocalization()`
- `app.UseRequestLocalization(...)` from those options

Notes that matter if you copy this:

- `IStringLocalizer<T>` is a **global catalog**. The type argument is unused (not per-class `.resx`).
- Missing key: return the key, `ResourceNotFound = true`.
- **Set `SupportedCultures`.** If you leave it empty, ASP.NET Core will ignore `Accept-Language` and stay on the default culture. TallyJ’s checked-in `appsettings.json` currently leaves this empty; do not copy that part.
- Implement **English fallback** in the provider (try `en` if the requested culture has no key). TallyJ’s comment claims this; the code does not. Copy the comment as a requirement, not the gap.
- The API host must **actually have that folder**. `csproj` copy-to-output, a Docker volume, or a deploy step. A relative `../frontend/src/locales` works on a laptop; it will not exist in a published API zip unless you put it there.

Root `locales/common.json` (language names, feature flags) is frontend-only. The backend only reads `{lang}/*.json`.

---

## Interpolation: do not mix placeholders

| Side | Syntax | Example |
|------|--------|---------|
| Vue (vue-i18n) | named | `"Are you sure you want to delete {item}?"` |
| C# `IStringLocalizer` | `string.Format` | `"Are you sure you want to delete {0}?"` |

Shared strings used on **both** sides should be unparameterized, or you maintain two keys.

Parameterized UI strings: keep them Vue-only, or use pattern (2) and send `{ item: name }` from the API.

If you call `_localizer["common.confirmDelete", itemName]` on a `{item}` string, `string.Format` will not fill it (and `{0}` in Vue will not either).

---

## Validation

A node script (`validate:i18n`) that:

- Parses every JSON file
- Rejects empty values and non-strings
- Rejects duplicate keys in a file and across files in one locale
- Optionally checks key-set equality across locales

Run it locally when you touch locales. Decide whether CI should fail on missing keys in other languages.

TallyJ’s **policy** is English-only adds; its **validator** wants every locale to have the same keys. Those two rules fight. Pick one:

- **A.** English-only until a translation pass: validator must allow extra English keys (and extra English files).
- **B.** Every locale gets the key immediately (empty value or copy-from-en as an explicit “needs translation” marker). Then key-set equality is fair.

Do not document A and enforce B.

---

## Minimal copy checklist

For a new .NET  + Vue repo:

1. `frontend/src/locales/en/*.json` — flat dotted keys, one file per area.
2. Vue: `flatToNested` → `createI18n`, fallback `en`, `Accept-Language` on the API client.
3. C#: `JsonStringLocalizer` reading `Localization:ResourcesPath`, `UseRequestLocalization`, **non-empty `SupportedCultures`**, English fallback on miss.
4. Two call patterns: lookup vs return-the-key. Default to return-the-key for UI.
5. Unparameterized strings if both sides translate the same key.
6. `validate:i18n` aligned with whether non-English files must stay in lockstep.
7. Prod: merge-locales for Vue chunks; **copy or mount** the locale folder onto the API host.
8. Never edit generated `bundled/` output.

---

## What TallyJ files to look at

| Path | Why |
|------|-----|
| `frontend/src/locales/index.ts` | vue-i18n setup, `flatToNested`, lazy locales |
| `frontend/src/locales/validate-translations.js` | validator |
| `frontend/merge-locales.js` | prod bundle |
| `frontend/src/api/config.ts` | `Accept-Language` |
| `backend/Localization/*.cs` | custom JSON localizer |
| `backend/Program.cs` + `Program.AppPipeline.cs` | registration + request localization |
| `backend/appsettings.json` | `Localization:ResourcesPath` |
| `backend/Services/Auth/LocalAuthService.cs` | pattern 1 |
| `backend/Enumerations/ElectionStageMessageKeys.cs` | pattern 2 |

Existing TallyJ docs (`README`, `AGENTS.md`, `frontend/README.md`) cover **contributor policy** (English-only, don’t touch `bundled/`, run `validate:i18n`). They do not explain sharing. This note is that missing piece.

---

## Policy TallyJ uses for new strings

When adding user-facing text, add it **only** to English. Other languages are updated in a later translation pass. Do not fill `fr`/`es`/… with English — that hides real translation work.

That policy is independent of the sharing architecture. You can copy the architecture and still choose a different translation workflow.
