import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RegisterPage from './RegisterPage.vue'
import { pinia, router, i18n } from '@/test/setup'
import ElementPlus from 'element-plus'

describe('RegisterPage', () => {
  it('renders register title', () => {
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus]
      }
    })
    expect(wrapper.text()).toContain('Register')
  })

  it('renders properly', () => {
    const wrapper = mount(RegisterPage, {
      global: {
        plugins: [pinia, router, i18n, ElementPlus]
      }
    })
    expect(wrapper.exists()).toBe(true)
  })
})