# Adding a New Language to TallyJ

This document outlines the steps required to add a new language to the TallyJ application.

## Prerequisites

- Language code (ISO 639-1 format, e.g., "ko" for Korean)
- Country flag code (ISO 3166-1 alpha-2 format, e.g., "kr" for South Korea)
- Native language name in the target language

## Steps

### 1. Create Language Directory and Stub Translations

Create a new directory under `frontend/src/locales/` with the language code:

```
frontend/src/locales/{lang}/
```

Create at least a `common.json` file with minimal translations:

```json
{
  "title": "TallyJ v4",
  "{lang}": "{Native Language Name}"
}
```

Example for Korean:
```json
{
  "title": "TallyJ v4",
  "korean": "한국어"
}
```

### 2. Update I18n Configuration

Edit `frontend/src/locales/index.ts`:

1. Add the language module import:
   ```typescript
   const {lang}Modules = import.meta.glob("./{lang}/*.json", { eager: true });
   ```

2. Add the language to the messages object:
   ```typescript
   messages: {
     en: deepMerge(common, mergeLocaleFiles(enModules)),
     fr: deepMerge(common, mergeLocaleFiles(frModules)),
     {lang}: deepMerge(common, mergeLocaleFiles({lang}Modules)),
   },
   ```

3. Update the setLocale function type:
   ```typescript
   export function setLocale(locale: "en" | "fr" | "{lang}") {
   ```

### 3. Update Common Translations

Add the language name to `frontend/src/locales/common.json`:

```json
{
  "title": "TallyJ v4",
  "english": "English",
  "french": "Français",
  "{lang}": "{Native Language Name}",
  ...
}
```

### 4. Add CSS Font Support

Add language-specific font-family rules to `frontend/src/style.css`:

```css
/* {Language Name} Language Support */
:lang({lang}) {
  font-family:
    "{Primary Font}",
    "{Secondary Font}",
    ...additional fonts...,
    var(--font-family-primary);
}
```

For Korean example:
```css
/* Korean Language Support */
:lang(ko) {
  font-family:
    "Noto Sans KR",
    "Malgun Gothic",
    "Apple Gothic",
    "Apple SD Gothic Neo",
    "Nanum Gothic",
    "돋움",
    "Dotum",
    "굴림",
    "Gulim",
    var(--font-family-primary);
}
```

### 5. Update Language Selector Component

Edit `frontend/src/components/common/LanguageSelector.vue`:

Add the new language to the languages array:

```typescript
const languages = [
  { value: "en", flag: "us", label: t("english") },
  { value: "fr", flag: "fr", label: t("french") },
  { value: "{lang}", flag: "{flag}", label: t("{lang}") },
];
```

## Validation

Run the translation validation script to ensure the new language is recognized:

```bash
cd frontend
npm run validate:i18n
```

## Notes

- Start with minimal translations (just the language name) and expand as needed
- Use appropriate country flags for the language selector
- Test font rendering with actual Korean/Hangul characters
- The application will fall back to English for any missing translations
- Consider right-to-left (RTL) language support if adding languages like Arabic or Hebrew

## Example: Adding Korean

Following the steps above with these values:
- Language code: `ko`
- Flag code: `kr`
- Native name: `한국어`

Results in Korean language support with South Korean flag in the language selector.