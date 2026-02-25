import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import DashboardPage from './DashboardPage.vue'
import { i18n } from '../test/setup'

describe('DashboardPage', () => {
  it('renders dashboard page', () => {
    const wrapper = mount(DashboardPage, {
      global: {
        plugins: [createTestingPinia(), i18n],
        stubs: ['el-card', 'el-row', 'el-col', 'el-icon']
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('displays statistics section', () => {
    const wrapper = mount(DashboardPage, {
      global: {
        plugins: [createTestingPinia(), i18n],
        stubs: ['el-card', 'el-row', 'el-col', 'el-icon']
      }
    })
    expect(wrapper.text()).toContain('dashboard.statistics')
  })
})