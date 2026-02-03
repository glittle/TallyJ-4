import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginPage from './LoginPage.vue'
import { pinia, router, i18n } from '@/test/setup'
import ElementPlus from 'element-plus'

describe('LoginPage', () => {
  it('renders login title', () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus]
      }
    })
    expect(wrapper.text()).toContain('Login')
  })

  it('renders properly', () => {
    const wrapper = mount(LoginPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus]
      }
    })
    expect(wrapper.exists()).toBe(true)
  })
})