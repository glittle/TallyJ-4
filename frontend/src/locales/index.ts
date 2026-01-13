import { createI18n } from 'vue-i18n'
import en from './en.json'
import fr from './fr.json'

const savedLocale = localStorage.getItem('preferred-language') || 'en'

export const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: {
    en,
    fr
  }
})

export function setLocale(locale: string) {
  i18n.global.locale.value = locale as any
  localStorage.setItem('preferred-language', locale)
}
