import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createApp } from 'vue'
import AppHeader from './AppHeader.vue'
import { pinia, router, i18n } from '../test/setup'

describe('AppHeader', () => {
  it('renders properly', () => {
    const app = createApp(AppHeader)
    app.use(pinia)
    app.use(router)
    app.use(i18n)

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n]
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('contains expected elements', () => {
    const app = createApp(AppHeader)
    app.use(pinia)
    app.use(router)
    app.use(i18n)

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [pinia, router, i18n]
      }
    })
    // Add more specific assertions based on the component structure
    expect(wrapper.html()).toContain('TallyJ 4')
  })
})