import { createI18n } from 'vue-i18n';
import en from './en.json';
import fr from './fr.json';

type Locale = 'en' | 'fr';

const messages = {
  en,
  fr
};

const getDefaultLocale = (): Locale => {
  const stored = localStorage.getItem('preferred-language');
  if (stored && (stored === 'en' || stored === 'fr')) {
    return stored as Locale;
  }
  const browserLang = navigator.language?.split('-')[0];
  return (browserLang === 'en' || browserLang === 'fr') ? browserLang : 'en';
};

export const i18n = createI18n<false>({
  legacy: false,
  locale: getDefaultLocale(),
  fallbackLocale: 'en',
  messages,
  globalInjection: true
});

export const setLocale = (locale: Locale) => {
  if (i18n.global.locale && typeof i18n.global.locale === 'object' && 'value' in i18n.global.locale) {
    i18n.global.locale.value = locale as any;
  }
  localStorage.setItem('preferred-language', locale);
  document.documentElement.setAttribute('lang', locale);
};
