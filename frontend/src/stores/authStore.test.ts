import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from './authStore'

// Mock the auth service
vi.mock('../services/authService', () => ({
  authService: {
    register: vi.fn(),
    login: vi.fn()
  }
}))

// Mock the API error handler
vi.mock('../composables/useApiErrorHandler', () => ({
  useApiErrorHandler: () => ({
    handleApiError: vi.fn()
  })
}))

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

describe('Auth Store', () => {
  let authStore: ReturnType<typeof useAuthStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    // Reset localStorage mock to return null by default
    localStorageMock.getItem.mockReturnValue(null)
    localStorageMock.setItem.mockImplementation(() => {})
    localStorageMock.removeItem.mockImplementation(() => {})
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('should initialize with null token and email when localStorage is empty', () => {
      localStorageMock.getItem.mockReturnValue(null)
      authStore = useAuthStore()

      expect(authStore.token).toBeNull()
      expect(authStore.email).toBeNull()
      expect(authStore.requires2FA).toBe(false)
      expect(authStore.pending2FAEmail).toBeNull()
      expect(authStore.isAuthenticated).toBe(false)
    })

    it('should initialize with stored values when localStorage has data', () => {
      localStorageMock.getItem.mockImplementation((key: string) => {
        if (key === 'auth_token') return 'stored-token'
        if (key === 'user_email') return 'stored@example.com'
        return null
      })

      const freshStore = useAuthStore()
      expect(freshStore.token).toBe('stored-token')
      expect(freshStore.email).toBe('stored@example.com')
      expect(freshStore.isAuthenticated).toBe(true)
    })
  })

  describe('register', () => {
    it('should register user successfully without 2FA', async () => {
      const { authService } = await import('../services/authService')
      const mockResponse = {
        token: 'new-token',
        email: 'test@example.com',
        requires2FA: false
      }

      authService.register.mockResolvedValue(mockResponse)
      authStore = useAuthStore()

      const registerData = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123'
      }

      const result = await authStore.register(registerData)

      expect(authService.register).toHaveBeenCalledWith(registerData)
      expect(authStore.token).toBe('new-token')
      expect(authStore.email).toBe('test@example.com')
      expect(authStore.requires2FA).toBe(false)
      expect(authStore.pending2FAEmail).toBeNull()
      expect(localStorageMock.setItem).toHaveBeenCalledWith('auth_token', 'new-token')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('user_email', 'test@example.com')
      expect(result).toEqual(mockResponse)
    })

    it('should handle 2FA registration', async () => {
      const { authService } = await import('../services/authService')
      const mockResponse = {
        email: 'test@example.com',
        requires2FA: true
      }

      authService.register.mockResolvedValue(mockResponse)
      authStore = useAuthStore()

      const registerData = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123'
      }

      const result = await authStore.register(registerData)

      expect(authStore.token).toBeNull()
      expect(authStore.email).toBeNull()
      expect(authStore.requires2FA).toBe(true)
      expect(authStore.pending2FAEmail).toBe('test@example.com')
      expect(localStorageMock.setItem).not.toHaveBeenCalled()
      expect(result).toEqual(mockResponse)
    })

    it('should handle registration errors', async () => {
      const { authService } = await import('../services/authService')
      const mockError = new Error('Registration failed')
      authService.register.mockRejectedValue(mockError)
      authStore = useAuthStore()

      const registerData = {
        username: 'testuser',
        email: 'test@example.com',
        password: 'password123',
        confirmPassword: 'password123'
      }

      await expect(authStore.register(registerData)).rejects.toThrow('Registration failed')
    })
  })

  describe('login', () => {
    it('should login user successfully without 2FA', async () => {
      const { authService } = await import('../services/authService')
      const mockResponse = {
        token: 'login-token',
        email: 'login@example.com',
        requires2FA: false
      }

      authService.login.mockResolvedValue(mockResponse)
      authStore = useAuthStore()

      const loginData = {
        email: 'login@example.com',
        password: 'password123'
      }

      const result = await authStore.login(loginData)

      expect(authService.login).toHaveBeenCalledWith(loginData)
      expect(authStore.token).toBe('login-token')
      expect(authStore.email).toBe('login@example.com')
      expect(authStore.requires2FA).toBe(false)
      expect(authStore.pending2FAEmail).toBeNull()
      expect(localStorageMock.setItem).toHaveBeenCalledWith('auth_token', 'login-token')
      expect(localStorageMock.setItem).toHaveBeenCalledWith('user_email', 'login@example.com')
      expect(result).toEqual(mockResponse)
    })

    it('should handle 2FA login', async () => {
      const { authService } = await import('../services/authService')
      const mockResponse = {
        requires2FA: true
      }

      authService.login.mockResolvedValue(mockResponse)
      authStore = useAuthStore()

      const loginData = {
        email: 'test@example.com',
        password: 'password123'
      }

      const result = await authStore.login(loginData)

      expect(authStore.token).toBeNull()
      expect(authStore.email).toBeNull()
      expect(authStore.requires2FA).toBe(true)
      expect(authStore.pending2FAEmail).toBe('test@example.com')
      expect(localStorageMock.setItem).not.toHaveBeenCalled()
      expect(result).toEqual(mockResponse)
    })

    it('should handle login errors', async () => {
      const { authService } = await import('../services/authService')
      const mockError = new Error('Login failed')
      authService.login.mockRejectedValue(mockError)
      authStore = useAuthStore()

      const loginData = {
        email: 'test@example.com',
        password: 'password123'
      }

      await expect(authStore.login(loginData)).rejects.toThrow('Login failed')
    })
  })

  describe('logout', () => {
    it('should clear all auth data and localStorage', () => {
      authStore = useAuthStore()
      // Set up initial state
      authStore.token = 'some-token'
      authStore.email = 'some@example.com'
      authStore.requires2FA = true
      authStore.pending2FAEmail = 'pending@example.com'

      authStore.logout()

      expect(authStore.token).toBeNull()
      expect(authStore.email).toBeNull()
      expect(authStore.requires2FA).toBe(false)
      expect(authStore.pending2FAEmail).toBeNull()
      expect(authStore.isAuthenticated).toBe(false)
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token')
      expect(localStorageMock.removeItem).toHaveBeenCalledWith('user_email')
    })
  })

  describe('computed properties', () => {
    it('should return true for isAuthenticated when token exists', () => {
      authStore = useAuthStore()
      authStore.token = 'some-token'
      expect(authStore.isAuthenticated).toBe(true)
    })

    it('should return false for isAuthenticated when token is null', () => {
      authStore = useAuthStore()
      authStore.token = null
      expect(authStore.isAuthenticated).toBe(false)
    })
  })
})