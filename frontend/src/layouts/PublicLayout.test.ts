import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import PublicLayout from './PublicLayout.vue'
import { i18n } from '../test/setup'
import { VERSION } from '../components/version'

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
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.exists()).toBe(true)
  })

  it('displays the application title', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.text()).toContain('TallyJ 4')
  })

  it('has the correct layout structure', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
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
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    const img = wrapper.find('img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('alt')).toBe('TallyJ Logo')
  })

  it('renders language selector and ThemeSelector components', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    // Either LanguageSelector or LanguageFlagsSelector is rendered based on route
    const hasLanguageSelector = wrapper.findComponent({ name: 'LanguageSelector' }).exists()
    const hasLanguageFlagsSelector = wrapper.findComponent({ name: 'LanguageFlagsSelector' }).exists()
    expect(hasLanguageSelector || hasLanguageFlagsSelector).toBe(true)
    expect(wrapper.findComponent({ name: 'ThemeSelector' }).exists()).toBe(true)
  })

  it('includes router-view for content', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    expect(wrapper.findComponent({ name: 'RouterView' }).exists()).toBe(true)
  })

  it('displays version tooltip on logo', () => {
    const wrapper = mount(PublicLayout, {
      global: {
        plugins: [router, i18n],
        stubs: {
          LanguageSelector: true,
          LanguageFlagsSelector: true,
          ThemeSelector: true
        }
      }
    })
    const logoH2 = wrapper.find('.logo h2')
    expect(logoH2.exists()).toBe(true)
    expect(logoH2.attributes('title')).toContain('Version')
    expect(logoH2.attributes('title')).toContain(VERSION)
  })
})