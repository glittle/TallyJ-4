import { createI18n } from 'vue-i18n'
import en from './en.json'
import fr from './fr.json'
import common from './common.json'

// Utility function to deep merge objects
function deepMerge(target: any, source: any): any {
  const result = { ...target }
  for (const key in source) {
    if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
      result[key] = deepMerge(result[key] || {}, source[key])
    } else {
      result[key] = source[key]
    }
  }
  return result
}

const savedLocale = localStorage.getItem('preferred-language') || 'en'

export const i18n = createI18n({
  locale: savedLocale,
  fallbackLocale: 'en',
  messages: {
    en: deepMerge(common, en),
    fr: deepMerge(common, fr)
  }
})

export function setLocale(locale: string) {
  i18n.global.locale = locale
  localStorage.setItem('preferred-language', locale)
}
