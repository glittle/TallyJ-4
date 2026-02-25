import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import App from './App.vue'
import { pinia, router } from './test/setup'

// Mock ErrorBoundary component
vi.mock('./components/common/ErrorBoundary.vue', () => ({
  default: {
    name: 'ErrorBoundary',
    template: '<div><slot /></div>'
  }
}))

describe('App', () => {

  it('renders properly', () => {
    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
        stubs: ['RouterView']
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('displays branch name when not on main branch', async () => {
    // Mock process.env.BRANCH_NAME to a non-main branch
    const originalBranchName = process.env.BRANCH_NAME
    process.env.BRANCH_NAME = 'feature/test-branch'

    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
        stubs: ['RouterView']
      }
    })

    await wrapper.vm.$nextTick()

    const branchElement = wrapper.find('.devBranchName')
    expect(branchElement.exists()).toBe(true)
    expect(branchElement.text()).toContain('Branch: feature/test-branch')

    // Restore original value
    process.env.BRANCH_NAME = originalBranchName
  })

  it('does not display branch name when on main branch', async () => {
    // Mock process.env.BRANCH_NAME to main
    const originalBranchName = process.env.BRANCH_NAME
    process.env.BRANCH_NAME = 'main'

    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
        stubs: ['RouterView']
      }
    })

    await wrapper.vm.$nextTick()

    const branchElement = wrapper.find('.devBranchName')
    expect(branchElement.exists()).toBe(false)

    // Restore original value
    process.env.BRANCH_NAME = originalBranchName
  })

  it('hides branch name when clicked', async () => {
    // Mock process.env.BRANCH_NAME to a non-main branch
    const originalBranchName = process.env.BRANCH_NAME
    process.env.BRANCH_NAME = 'feature/test-branch'

    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
        stubs: ['RouterView']
      }
    })

    await wrapper.vm.$nextTick()

    let branchElement = wrapper.find('.devBranchName')
    expect(branchElement.exists()).toBe(true)

    // Click on the branch element
    await branchElement.trigger('click')
    await wrapper.vm.$nextTick()

    branchElement = wrapper.find('.devBranchName')
    expect(branchElement.exists()).toBe(false)

    // Restore original value
    process.env.BRANCH_NAME = originalBranchName
  })

  it('has correct title attribute on branch display', async () => {
    // Mock process.env.BRANCH_NAME to a non-main branch
    const originalBranchName = process.env.BRANCH_NAME
    process.env.BRANCH_NAME = 'feature/test-branch'

    const wrapper = mount(App, {
      global: {
        plugins: [pinia, router],
        stubs: ['RouterView']
      }
    })

    await wrapper.vm.$nextTick()

    const branchElement = wrapper.find('.devBranchName')
    expect(branchElement.attributes('title')).toBe('Click to remove')

    // Restore original value
    process.env.BRANCH_NAME = originalBranchName
  })
})
