import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useThemeStore } from './themeStore'

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn()
}

// Mock localStorage globally before tests
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true
})

describe('Theme Store', () => {
  let themeStore: ReturnType<typeof useThemeStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    // Reset localStorage mock to return null by default
    localStorageMock.getItem.mockReturnValue(null)
    localStorageMock.setItem.mockImplementation(() => {})
    
    // Mock document.documentElement
    const mockClassList = {
      add: vi.fn(),
      remove: vi.fn()
    }
    Object.defineProperty(document, 'documentElement', {
      value: {
        classList: mockClassList
      },
      writable: true,
      configurable: true
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('should initialize with light theme when localStorage is empty', () => {
      localStorageMock.getItem.mockReturnValue(null)
      themeStore = useThemeStore()

      expect(themeStore.theme).toBe('light')
      expect(localStorageMock.getItem).toHaveBeenCalledWith('theme')
    })

    it('should initialize with dark theme when localStorage has dark', () => {
      localStorageMock.getItem.mockReturnValue('dark')
      themeStore = useThemeStore()

      expect(themeStore.theme).toBe('dark')
      expect(localStorageMock.getItem).toHaveBeenCalledWith('theme')
    })

    it('should initialize with light theme when localStorage has light', () => {
      localStorageMock.getItem.mockReturnValue('light')
      themeStore = useThemeStore()

      expect(themeStore.theme).toBe('light')
      expect(localStorageMock.getItem).toHaveBeenCalledWith('theme')
    })

    it('should apply theme classes to document on initialization', () => {
      localStorageMock.getItem.mockReturnValue('dark')
      themeStore = useThemeStore()

      expect(document.documentElement.classList.add).toHaveBeenCalledWith('dark')
    })
  })

  describe('setTheme', () => {
    it('should set theme to dark and save to localStorage', () => {
      localStorageMock.getItem.mockReturnValue(null)
      themeStore = useThemeStore()

      themeStore.setTheme('dark')

      expect(themeStore.theme).toBe('dark')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('theme', 'dark')
      expect(document.documentElement.classList.add).toHaveBeenCalledWith('dark')
    })

    it('should set theme to light and save to localStorage', () => {
      localStorageMock.getItem.mockReturnValue('dark')
      themeStore = useThemeStore()

      themeStore.setTheme('light')

      expect(themeStore.theme).toBe('light')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('theme', 'light')
      expect(document.documentElement.classList.remove).toHaveBeenCalledWith('dark')
    })
  })

  describe('toggleTheme', () => {
    it('should toggle from light to dark', () => {
      localStorageMock.getItem.mockReturnValue('light')
      themeStore = useThemeStore()

      themeStore.toggleTheme()

      expect(themeStore.theme).toBe('dark')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('theme', 'dark')
    })

    it('should toggle from dark to light', () => {
      localStorageMock.getItem.mockReturnValue('dark')
      themeStore = useThemeStore()

      themeStore.toggleTheme()

      expect(themeStore.theme).toBe('light')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('theme', 'light')
    })
  })

  describe('theme persistence', () => {
    it('should persist theme across store re-initialization', () => {
      // First initialization - set dark theme
      localStorageMock.getItem.mockReturnValue(null)
      themeStore = useThemeStore()
      themeStore.setTheme('dark')

      expect(localStorageMock.setItem).toHaveBeenCalledWith('theme', 'dark')

      // Simulate page reload - new store instance
      localStorageMock.getItem.mockReturnValue('dark')
      const newThemeStore = useThemeStore()

      expect(newThemeStore.theme).toBe('dark')
      expect(document.documentElement.classList.add).toHaveBeenCalledWith('dark')
    })
  })
})
