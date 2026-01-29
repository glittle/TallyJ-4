import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginPage from './LoginPage.vue'

describe('LoginPage', () => {
  it('renders login title', () => {
    const wrapper = mount(LoginPage)
    expect(wrapper.text()).toContain('Login')
  })

  it('renders properly', () => {
    const wrapper = mount(LoginPage)
    expect(wrapper.exists()).toBe(true)
  })
})