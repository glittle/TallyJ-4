import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import PublicLayout from './PublicLayout.vue'

describe('PublicLayout', () => {
  let router: any

  beforeEach(() => {
    router = createRouter({
      history: createWebHistory(),
      routes: []
    })
  })

  it('renders properly', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('displays the application title', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.text()).toContain('TallyJ 4')
  })

  it('has the correct layout structure', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.classes()).toContain('public-layout')
    expect(wrapper.find('.public-header').exists()).toBe(true)
    expect(wrapper.find('.public-content').exists()).toBe(true)
  })

  it('includes the logo image', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    const img = wrapper.find('img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('alt')).toBe('TallyJ Logo')
  })

  it('renders LanguageSelector and ThemeSelector components', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.findComponent({ name: 'LanguageSelector' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'ThemeSelector' }).exists()).toBe(true)
  })

  it('includes router-view for content', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router],
        stubs: {
          LanguageSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.findComponent({ name: 'RouterView' }).exists()).toBe(true)
  })
})