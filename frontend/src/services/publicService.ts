import api from './api'
import type { PublicDisplayDto } from '../types'

export const publicService = {
  async getPublicDisplay(electionGuid: string): Promise<PublicDisplayDto> {
    const response = await api.get<PublicDisplayDto>(`/public/elections/${electionGuid}/display`)
    return response.data
  }
}
