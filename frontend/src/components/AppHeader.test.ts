import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createApp } from 'vue'
import AppHeader from './AppHeader.vue'

describe('AppHeader', () => {
  it('renders properly', () => {
    const app = createApp(AppHeader)
    app.use((global as any).pinia)
    app.use((global as any).router)
    app.use((global as any).i18n)

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [(global as any).pinia, (global as any).router, (global as any).i18n]
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('contains expected elements', () => {
    const app = createApp(AppHeader)
    app.use((global as any).pinia)
    app.use((global as any).router)
    app.use((global as any).i18n)

    const wrapper = mount(AppHeader, {
      global: {
        plugins: [(global as any).pinia, (global as any).router, (global as any).i18n]
      }
    })
    // Add more specific assertions based on the component structure
    expect(wrapper.html()).toContain('TallyJ 4')
  })
})