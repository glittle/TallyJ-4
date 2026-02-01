<template>
  <div class="locations-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-page-header @back="goBack" content="Voting Locations" />
          </div>
          <div class="header-actions">
            <el-button type="primary" @click="showCreateDialog = true">
              <el-icon><Plus /></el-icon>
              Add Location
            </el-button>
          </div>
        </div>
      </template>

      <div class="table-container">
        <el-table
          :data="sortedLocations"
          v-loading="loading"
          style="width: 100%"
          @sort-change="handleSortChange"
        >
          <el-table-column prop="name" label="Location Name" min-width="200" sortable="custom" />
          <el-table-column prop="contactInfo" label="Contact Info" min-width="200" />
          <el-table-column prop="tallyStatus" label="Tally Status" width="150">
            <template #default="scope">
              <el-tag v-if="scope.row.tallyStatus" :type="getStatusType(scope.row.tallyStatus)">
                {{ scope.row.tallyStatus }}
              </el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column prop="ballotsCollected" label="Ballots" width="100" align="center" sortable="custom">
            <template #default="scope">
              {{ scope.row.ballotsCollected || 0 }}
            </template>
          </el-table-column>
          <el-table-column prop="sortOrder" label="Sort Order" width="120" align="center" sortable="custom">
            <template #default="scope">
              {{ scope.row.sortOrder ?? '-' }}
            </template>
          </el-table-column>
          <el-table-column label="Coordinates" width="180">
            <template #default="scope">
              <span v-if="scope.row.longitude && scope.row.latitude" class="coordinates">
                {{ formatCoordinate(scope.row.longitude) }}, {{ formatCoordinate(scope.row.latitude) }}
              </span>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="180" fixed="right">
            <template #default="scope">
              <el-button-group>
                <el-button size="small" @click="editLocation(scope.row)">
                  <el-icon><Edit /></el-icon>
                  Edit
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  @click="deleteLocation(scope.row)"
                >
                  <el-icon><Delete /></el-icon>
                  Delete
                </el-button>
              </el-button-group>
            </template>
          </el-table-column>
        </el-table>

        <div class="pagination-container" v-if="pagination.totalPages > 1">
          <el-pagination
            v-model:current-page="pagination.pageNumber"
            v-model:page-size="pagination.pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="pagination.totalCount"
            layout="total, sizes, prev, pager, next"
            @size-change="handleSizeChange"
            @current-change="handlePageChange"
          />
        </div>
      </div>
    </el-card>

    <LocationFormDialog
      v-model="showCreateDialog"
      :election-guid="electionGuid"
      @success="handleFormSuccess"
    />

    <LocationFormDialog
      v-model="showEditDialog"
      :election-guid="electionGuid"
      :location="editingLocation"
      :is-edit="true"
      @success="handleFormSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Plus, Edit, Delete } from '@element-plus/icons-vue';
import { useLocationStore } from '../../stores/locationStore';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { LocationDto } from '../../types';
import LocationFormDialog from '../../components/locations/LocationFormDialog.vue';

const router = useRouter();
const route = useRoute();
const locationStore = useLocationStore();

const electionGuid = route.params.id as string;
const showCreateDialog = ref(false);
const showEditDialog = ref(false);
const editingLocation = ref<LocationDto | null>(null);

const loading = computed(() => locationStore.loading);
const sortedLocations = computed(() => locationStore.sortedLocations);
const pagination = computed(() => locationStore.pagination);

const sort = ref({
  prop: 'sortOrder',
  order: 'ascending' as 'ascending' | 'descending'
});

onMounted(async () => {
  await loadLocations();
});

async function loadLocations() {
  try {
    await locationStore.fetchLocations(electionGuid, pagination.value.pageNumber, pagination.value.pageSize);
  } catch (error) {
    ElMessage.error('Failed to load locations');
  }
}

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function editLocation(location: LocationDto) {
  editingLocation.value = location;
  showEditDialog.value = true;
}

async function deleteLocation(location: LocationDto) {
  try {
    await ElMessageBox.confirm(
      `Are you sure you want to delete location "${location.name}"?`,
      'Warning',
      {
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        type: 'warning'
      }
    );

    await locationStore.deleteLocation(electionGuid, location.locationGuid);
    ElMessage.success('Location deleted successfully');
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || 'Failed to delete location');
    }
  }
}

function handleFormSuccess() {
  showCreateDialog.value = false;
  showEditDialog.value = false;
  editingLocation.value = null;
  loadLocations();
}

function handleSortChange({ prop, order }: any) {
  sort.value.prop = prop;
  sort.value.order = order;
}

async function handleSizeChange() {
  await loadLocations();
}

async function handlePageChange() {
  await loadLocations();
}

function formatCoordinate(coord: string | undefined): string {
  if (!coord) return '';
  const num = parseFloat(coord);
  return isNaN(num) ? coord : num.toFixed(4);
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    'NotStarted': '',
    'InProgress': 'warning',
    'Completed': 'success',
    'Verified': 'info'
  };
  return typeMap[status] || '';
}
</script>

<style scoped>
.locations-list-page {
  padding: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left {
  flex: 1;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.table-container {
  margin-top: 20px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.coordinates {
  font-family: monospace;
  font-size: 0.9em;
}
</style>
