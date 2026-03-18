import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { computerService } from "../services/computerService";
import { locationService } from "../services/locationService";
import type {
  ComputerDto,
  CreateLocationDto,
  LocationDto,
  RegisterComputerDto,
  UpdateLocationDto,
} from "../types";

export const SELECTED_LOCATION_KEY = "tallyj_selected_location";

export const useLocationStore = defineStore("location", () => {
  const locations = ref<LocationDto[]>([]);
  const currentLocation = ref<LocationDto | null>(null);
  const computers = ref<ComputerDto[]>([]);
  const loading = ref(false);
  const computersLoading = ref(false);
  const error = ref<string | null>(null);
  const pagination = ref({
    pageNumber: 1,
    pageSize: 50,
    totalCount: 0,
    totalPages: 0,
  });

  // Persistent location selection (survives page refresh until logout)
  const selectedLocationGuid = ref<string | null>(null);

  // Initialize from localStorage
  const initializeSelectedLocation = () => {
    try {
      const stored = localStorage.getItem(SELECTED_LOCATION_KEY);
      if (stored) {
        selectedLocationGuid.value = stored;
      }
    } catch (e) {
      console.error("Failed to load selected location from localStorage:", e);
    }
  };

  // Save to localStorage whenever it changes
  const persistSelectedLocation = (locationGuid: string | null) => {
    try {
      if (locationGuid) {
        localStorage.setItem(SELECTED_LOCATION_KEY, locationGuid);
      } else {
        localStorage.removeItem(SELECTED_LOCATION_KEY);
      }
    } catch (e) {
      console.error("Failed to save selected location to localStorage:", e);
    }
  };

  // Initialize on store creation
  initializeSelectedLocation();

  const sortedLocations = computed(() =>
    [...locations.value].sort((a, b) => {
      if (a.sortOrder !== b.sortOrder) {
        return (a.sortOrder || 0) - (b.sortOrder || 0);
      }
      return a.name.localeCompare(b.name);
    }),
  );

  async function fetchLocations(
    electionGuid: string,
    pageNumber = 1,
    pageSize = 50,
  ) {
    loading.value = true;
    error.value = null;
    try {
      const response = await locationService.getAll(
        electionGuid,
        pageNumber,
        pageSize,
      );
      locations.value = response.items;
      pagination.value = {
        pageNumber: response.pageNumber,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
        totalPages: response.totalPages,
      };
    } catch (e: any) {
      error.value = e.message || "Failed to fetch locations";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function fetchLocationById(electionGuid: string, locationGuid: string) {
    loading.value = true;
    error.value = null;
    try {
      const location = await locationService.getById(
        electionGuid,
        locationGuid,
      );
      currentLocation.value = location;

      const index = locations.value.findIndex(
        (l) => l.locationGuid === locationGuid,
      );
      if (index !== -1) {
        locations.value[index] = location;
      } else {
        locations.value.push(location);
      }

      return location;
    } catch (e: any) {
      error.value = e.message || "Failed to fetch location";
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
      error.value = e.message || "Failed to create location";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  async function updateLocation(
    electionGuid: string,
    locationGuid: string,
    dto: UpdateLocationDto,
  ) {
    loading.value = true;
    error.value = null;
    try {
      const location = await locationService.update(
        electionGuid,
        locationGuid,
        dto,
      );

      const index = locations.value.findIndex(
        (l) => l.locationGuid === locationGuid,
      );
      if (index !== -1) {
        locations.value[index] = location;
      }

      if (currentLocation.value?.locationGuid === locationGuid) {
        currentLocation.value = location;
      }

      return location;
    } catch (e: any) {
      error.value = e.message || "Failed to update location";
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

      locations.value = locations.value.filter(
        (l) => l.locationGuid !== locationGuid,
      );

      if (currentLocation.value?.locationGuid === locationGuid) {
        currentLocation.value = null;
      }

      pagination.value.totalCount--;
    } catch (e: any) {
      error.value = e.message || "Failed to delete location";
      throw e;
    } finally {
      loading.value = false;
    }
  }

  function setCurrentLocation(location: LocationDto | null) {
    currentLocation.value = location;
  }

  function selectLocation(locationGuid: string | null) {
    selectedLocationGuid.value = locationGuid;
    persistSelectedLocation(locationGuid);

    // Update currentLocation if the location is already loaded
    if (locationGuid) {
      const location = locations.value.find(
        (l) => l.locationGuid === locationGuid,
      );
      if (location) {
        currentLocation.value = location;
      }
    } else {
      currentLocation.value = null;
    }
  }

  function clearSelectedLocation() {
    selectedLocationGuid.value = null;
    persistSelectedLocation(null);
  }

  function clearError() {
    error.value = null;
  }

  function clearLocations() {
    locations.value = [];
    currentLocation.value = null;
    computers.value = [];
    pagination.value = {
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0,
    };
  }

  async function fetchComputers(electionGuid: string, locationGuid: string) {
    computersLoading.value = true;
    error.value = null;
    try {
      computers.value = await computerService.getByLocation(
        electionGuid,
        locationGuid,
      );
    } catch (e: any) {
      error.value = e.message || "Failed to fetch computers";
      throw e;
    } finally {
      computersLoading.value = false;
    }
  }

  async function registerComputer(
    electionGuid: string,
    locationGuid: string,
    dto: RegisterComputerDto,
  ) {
    computersLoading.value = true;
    error.value = null;
    try {
      const computer = await computerService.register(
        electionGuid,
        locationGuid,
        dto,
      );
      computers.value.push(computer);
      return computer;
    } catch (e: any) {
      error.value = e.message || "Failed to register computer";
      throw e;
    } finally {
      computersLoading.value = false;
    }
  }

  async function deleteComputer(
    electionGuid: string,
    locationGuid: string,
    computerGuid: string,
  ) {
    computersLoading.value = true;
    error.value = null;
    try {
      await computerService.delete(electionGuid, locationGuid, computerGuid);
      computers.value = computers.value.filter(
        (c) => c.computerGuid !== computerGuid,
      );
    } catch (e: any) {
      error.value = e.message || "Failed to delete computer";
      throw e;
    } finally {
      computersLoading.value = false;
    }
  }

  return {
    locations,
    currentLocation,
    computers,
    loading,
    computersLoading,
    error,
    pagination,
    sortedLocations,
    selectedLocationGuid,
    fetchLocations,
    fetchLocationById,
    createLocation,
    updateLocation,
    deleteLocation,
    setCurrentLocation,
    selectLocation,
    clearSelectedLocation,
    clearError,
    clearLocations,
    fetchComputers,
    registerComputer,
    deleteComputer,
  };
});
