import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import RegisterPage from './RegisterPage.vue'

describe('RegisterPage', () => {
  it('renders register title', () => {
    const wrapper = mount(RegisterPage)
    expect(wrapper.text()).toContain('Register')
  })

  it('renders properly', () => {
    const wrapper = mount(RegisterPage)
    expect(wrapper.exists()).toBe(true)
  })
})