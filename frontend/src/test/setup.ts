import { beforeAll } from 'vitest'
import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import { createI18n } from 'vue-i18n'
import ElementPlus from 'element-plus'

// Setup for Vue 3 + Vitest
beforeAll(() => {
  // Global test setup can go here
  console.log('Setting up tests...')
})

// Mock Element Plus components
global.ElementPlus = ElementPlus

// Create global instances for testing
global.pinia = createPinia()
global.router = createRouter({
  history: createWebHistory(),
  routes: []
})
global.i18n = createI18n({
  locale: 'en',
  messages: {
    en: {},
    fr: {}
  }
})