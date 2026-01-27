import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useElectionStore } from './electionStore'
import type { ElectionDto, CreateElectionDto } from '../types'

// Mock the election service
vi.mock('../services/electionService', () => ({
  electionService: {
    getAll: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn()
  }
}))

// Mock the SignalR service
vi.mock('../services/signalrService', () => ({
  signalrService: {
    connectToMainHub: vi.fn(),
    joinElection: vi.fn(),
    leaveElection: vi.fn()
  }
}))

// Mock Element Plus
vi.mock('element-plus', () => ({
  ElMessage: {
    success: vi.fn(),
    info: vi.fn()
  }
}))

describe('Election Store', () => {
  let electionStore: ReturnType<typeof useElectionStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    electionStore = useElectionStore()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('should initialize with empty state', () => {
      expect(electionStore.elections).toEqual([])
      expect(electionStore.currentElection).toBeNull()
      expect(electionStore.loading).toBe(false)
      expect(electionStore.error).toBeNull()
      // signalrInitialized is not exposed in the return statement, so we can't test it directly
    })
  })

  describe('computed properties', () => {
    it('should filter active elections correctly', () => {
      const mockElections: ElectionDto[] = [
        { electionGuid: '1', tallyStatus: 'NotStarted' } as ElectionDto,
        { electionGuid: '2', tallyStatus: 'Tallying' } as ElectionDto,
        { electionGuid: '3', tallyStatus: 'Finalized' } as ElectionDto,
        { electionGuid: '4', tallyStatus: 'Archived' } as ElectionDto
      ]

      electionStore.elections = mockElections

      expect(electionStore.activeElections).toHaveLength(2)
      expect(electionStore.activeElections.map(e => e.electionGuid)).toEqual(['1', '2'])
    })

    it('should filter finalized elections correctly', () => {
      const mockElections: ElectionDto[] = [
        { electionGuid: '1', tallyStatus: 'NotStarted' } as ElectionDto,
        { electionGuid: '2', tallyStatus: 'Finalized' } as ElectionDto,
        { electionGuid: '3', tallyStatus: 'Finalized' } as ElectionDto
      ]

      electionStore.elections = mockElections

      expect(electionStore.finalizedElections).toHaveLength(2)
      expect(electionStore.finalizedElections.map(e => e.electionGuid)).toEqual(['2', '3'])
    })
  })

  describe('fetchElections', () => {
    it('should fetch elections successfully', async () => {
      const { electionService } = await import('../services/electionService')
      const mockElections: ElectionDto[] = [
        { electionGuid: '1', name: 'Election 1' } as ElectionDto,
        { electionGuid: '2', name: 'Election 2' } as ElectionDto
      ]

      electionService.getAll.mockResolvedValue(mockElections)

      await electionStore.fetchElections()

      expect(electionService.getAll).toHaveBeenCalled()
      expect(electionStore.elections).toEqual(mockElections)
      expect(electionStore.loading).toBe(false)
      expect(electionStore.error).toBeNull()
    })

    it('should handle fetch elections error', async () => {
      const { electionService } = await import('../services/electionService')
      const mockError = new Error('Network error')

      electionService.getAll.mockRejectedValue(mockError)

      await expect(electionStore.fetchElections()).rejects.toThrow('Network error')
      expect(electionStore.loading).toBe(false)
      expect(electionStore.error).toBe('Network error')
    })
  })

  describe('fetchElectionById', () => {
    it('should fetch election by id and update existing election', async () => {
      const { electionService } = await import('../services/electionService')
      const existingElection = { electionGuid: '1', name: 'Old Name' } as ElectionDto
      const updatedElection = { electionGuid: '1', name: 'New Name' } as ElectionDto

      electionStore.elections = [existingElection]
      electionService.getById.mockResolvedValue(updatedElection)

      await electionStore.fetchElectionById('1')

      expect(electionService.getById).toHaveBeenCalledWith('1')
      expect(electionStore.currentElection).toEqual(updatedElection)
      expect(electionStore.elections[0]).toEqual(updatedElection)
      expect(electionStore.loading).toBe(false)
    })

    it('should add new election if not in list', async () => {
      const { electionService } = await import('../services/electionService')
      const newElection = { electionGuid: '2', name: 'New Election' } as ElectionDto

      electionStore.elections = [{ electionGuid: '1', name: 'Existing' } as ElectionDto]
      electionService.getById.mockResolvedValue(newElection)

      await electionStore.fetchElectionById('2')

      expect(electionStore.elections).toHaveLength(2)
      expect(electionStore.elections[1]).toEqual(newElection)
    })
  })

  describe('createElection', () => {
    it('should create election successfully', async () => {
      const { electionService } = await import('../services/electionService')
      const createDto: CreateElectionDto = {
        name: 'New Election',
        numberOfWinners: 1,
        numberOfExtra: 0,
        electionType: 'Normal',
        electionMode: 'Normal'
      }
      const createdElection: ElectionDto = {
        electionGuid: 'new-id',
        name: 'New Election',
        numberOfWinners: 1,
        numberOfExtra: 0,
        electionType: 'Normal',
        electionMode: 'Normal',
        tallyStatus: 'NotStarted',
        electionStatus: 'NotStarted'
      } as ElectionDto

      electionService.create.mockResolvedValue(createdElection)

      const result = await electionStore.createElection(createDto)

      expect(electionService.create).toHaveBeenCalledWith(createDto)
      expect(electionStore.elections).toHaveLength(1)
      expect(electionStore.elections[0]).toEqual(createdElection)
      expect(electionStore.currentElection).toEqual(createdElection)
      expect(result).toEqual(createdElection)
    })
  })

  describe('updateElection', () => {
    it('should update existing election', async () => {
      const { electionService } = await import('../services/electionService')
      const existingElection = { electionGuid: '1', name: 'Old Name' } as ElectionDto
      const updatedElection = { electionGuid: '1', name: 'Updated Name' } as ElectionDto

      electionStore.elections = [existingElection]
      electionStore.currentElection = existingElection
      electionService.update.mockResolvedValue(updatedElection)

      const result = await electionStore.updateElection('1', { name: 'Updated Name' })

      expect(electionService.update).toHaveBeenCalledWith('1', { name: 'Updated Name' })
      expect(electionStore.elections[0]).toEqual(updatedElection)
      expect(electionStore.currentElection).toEqual(updatedElection)
      expect(result).toEqual(updatedElection)
    })
  })

  describe('deleteElection', () => {
    it('should delete election and clear current election if it matches', async () => {
      const { electionService } = await import('../services/electionService')
      const election1 = { electionGuid: '1', name: 'Election 1' } as ElectionDto
      const election2 = { electionGuid: '2', name: 'Election 2' } as ElectionDto

      electionStore.elections = [election1, election2]
      electionStore.currentElection = election1

      electionService.delete.mockResolvedValue(undefined)

      await electionStore.deleteElection('1')

      expect(electionService.delete).toHaveBeenCalledWith('1')
      expect(electionStore.elections).toHaveLength(1)
      expect(electionStore.elections[0]).toEqual(election2)
      expect(electionStore.currentElection).toBeNull()
    })
  })

  describe('setCurrentElection', () => {
    it('should set current election', () => {
      const election = { electionGuid: '1', name: 'Test Election' } as ElectionDto

      electionStore.setCurrentElection(election)

      expect(electionStore.currentElection).toEqual(election)
    })

    it('should clear current election when passed null', () => {
      electionStore.setCurrentElection(null)

      expect(electionStore.currentElection).toBeNull()
    })
  })

  describe('clearError', () => {
    it('should clear error state', () => {
      electionStore.error = 'Some error'

      electionStore.clearError()

      expect(electionStore.error).toBeNull()
    })
  })

  describe('initializeSignalR', () => {
    it('should initialize SignalR connection and set up event handlers', async () => {
      const { signalrService } = await import('../services/signalrService')
      const mockConnection = {
        on: vi.fn()
      }

      signalrService.connectToMainHub.mockResolvedValue(mockConnection)

      await electionStore.initializeSignalR()

      expect(signalrService.connectToMainHub).toHaveBeenCalled()
      expect(mockConnection.on).toHaveBeenCalledWith('ElectionUpdated', expect.any(Function))
      expect(mockConnection.on).toHaveBeenCalledWith('ElectionStatusChanged', expect.any(Function))
    })

    it('should handle SignalR initialization errors gracefully', async () => {
      const { signalrService } = await import('../services/signalrService')
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      signalrService.connectToMainHub.mockRejectedValue(new Error('Connection failed'))

      await electionStore.initializeSignalR()

      expect(consoleSpy).toHaveBeenCalledWith('Failed to initialize SignalR for election store:', expect.any(Error))
      consoleSpy.mockRestore()
    })
  })

  describe('joinElection and leaveElection', () => {
    it('should join election group', async () => {
      const { signalrService } = await import('../services/signalrService')

      await electionStore.joinElection('election-123')

      expect(signalrService.joinElection).toHaveBeenCalledWith('election-123')
    })

    it('should leave election group', async () => {
      const { signalrService } = await import('../services/signalrService')

      await electionStore.leaveElection('election-123')

      expect(signalrService.leaveElection).toHaveBeenCalledWith('election-123')
    })
  })
})