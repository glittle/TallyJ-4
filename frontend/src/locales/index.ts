import { createI18n } from 'vue-i18n'
import en from './en.json'
import fr from './fr.json'

const savedLocale = localStorage.getItem('preferred-language') || 'en'

export const i18n = createI18n({
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: {
    en,
    fr
  }
})

export function setLocale(locale: string) {
  i18n.global.locale = locale
  localStorage.setItem('preferred-language', locale)
}
