import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import DashboardPage from './DashboardPage.vue'

// Mock i18n
vi.mock('vue-i18n', () => ({
  useI18n: () => ({
    t: (key: string) => key
  })
}))

describe('DashboardPage', () => {
  it('renders dashboard page', () => {
    const wrapper = mount(DashboardPage, {
      global: {
        plugins: [createTestingPinia()],
        stubs: ['el-card', 'el-row', 'el-col', 'el-icon'],
        mocks: {
          $t: (key: string) => key
        }
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('displays statistics section', () => {
    const wrapper = mount(DashboardPage, {
      global: {
        plugins: [createTestingPinia()],
        stubs: ['el-card', 'el-row', 'el-col', 'el-icon'],
        mocks: {
          $t: (key: string) => key
        }
      }
    })
    expect(wrapper.text()).toContain('dashboard.statistics')
  })
})