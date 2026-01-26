import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import { createI18n } from 'vue-i18n'
import ElementPlus from 'element-plus'

// Create global instances for testing
const testPinia = createPinia()
const testRouter = createRouter({
  history: createWebHistory(),
  routes: []
})
const testI18n = createI18n({
  locale: 'en',
  messages: {
    en: {},
    fr: {}
  }
})

// Mock Element Plus components
;(globalThis as any).ElementPlus = ElementPlus
;(globalThis as any).pinia = testPinia
;(globalThis as any).router = testRouter
;(globalThis as any).i18n = testI18n

// Export for direct imports if needed
export { testPinia as pinia, testRouter as router, testI18n as i18n }