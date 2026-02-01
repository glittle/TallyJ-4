import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { locationService } from '../services/locationService';
import type { LocationDto, CreateLocationDto, UpdateLocationDto } from '../types';

export const useLocationStore = defineStore('location', () => {
  const locations = ref<LocationDto[]>([]);
  const currentLocation = ref<LocationDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const pagination = ref({
    pageNumber: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0
  });

  const sortedLocations = computed(() => 
    [...locations.value].sort((a, b) => {
      if (a.sortOrder !== b.sortOrder) {
        return (a.sortOrder || 0) - (b.sortOrder || 0);
      }
      return a.name.localeCompare(b.name);
    })
  );

  async function fetchLocations(electionGuid: string, pageNumber = 1, pageSize = 50) {
    loading.value = true;
    error.value = null;
    try {
      const response = await locationService.getAll(electionGuid, pageNumber, pageSize);
      locations.value = response.data;
      pagination.value = {
        pageNumber: response.pageNumber,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
        totalPages: response.totalPages
      };
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch locations';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchLocationById(electionGuid: string, locationGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const location = await locationService.getById(electionGuid, locationGuid);
      currentLocation.value = location;
      
      const index = locations.value.findIndex(l => l.locationGuid === locationGuid);
      if (index !== -1) {
        locations.value[index] = location;
      } else {
        locations.value.push(location);
      }
      
      return location;
    } catch (e: any) {
      error.value = e.message || 'Failed to fetch location';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function createLocation(electionGuid: string, dto: CreateLocationDto) {
    loading.value = true;
    error.value = null;
    try {
      const location = await locationService.create(electionGuid, dto);
      locations.value.push(location);
      currentLocation.value = location;
      pagination.value.totalCount++;
      return location;
    } catch (e: any) {
      error.value = e.message || 'Failed to create location';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateLocation(electionGuid: string, locationGuid: string, dto: UpdateLocationDto) {
    loading.value = true;
    error.value = null;
    try {
      const location = await locationService.update(electionGuid, locationGuid, dto);
      
      const index = locations.value.findIndex(l => l.locationGuid === locationGuid);
      if (index !== -1) {
        locations.value[index] = location;
      }
      
      if (currentLocation.value?.locationGuid === locationGuid) {
        currentLocation.value = location;
      }
      
      return location;
    } catch (e: any) {
      error.value = e.message || 'Failed to update location';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function deleteLocation(electionGuid: string, locationGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      await locationService.delete(electionGuid, locationGuid);
      
      locations.value = locations.value.filter(l => l.locationGuid !== locationGuid);
      
      if (currentLocation.value?.locationGuid === locationGuid) {
        currentLocation.value = null;
      }
      
      pagination.value.totalCount--;
    } catch (e: any) {
      error.value = e.message || 'Failed to delete location';
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentLocation(location: LocationDto | null) {
    currentLocation.value = location;
  }

  function clearError() {
    error.value = null;
  }

  function clearLocations() {
    locations.value = [];
    currentLocation.value = null;
    pagination.value = {
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0
    };
  }

  return {
    locations,
    currentLocation,
    loading,
    error,
    pagination,
    sortedLocations,
    fetchLocations,
    fetchLocationById,
    createLocation,
    updateLocation,
    deleteLocation,
    setCurrentLocation,
    clearError,
    clearLocations
  };
});
