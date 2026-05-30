<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { Delete, Edit, Monitor, Plus } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import ComputerRegistrationDialog from "../../components/locations/ComputerRegistrationDialog.vue";
import LocationFormDialog from "../../components/locations/LocationFormDialog.vue";
import { useLocationStore } from "../../stores/locationStore";
import type { ComputerDto, LocationDto } from "../../types";

const router = useRouter();
const route = useRoute();
const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { t } = useI18n();

const electionGuid = route.params.id as string;
const showCreateDialog = ref(false);
const showEditDialog = ref(false);
const editingLocation = ref<LocationDto | null>(null);
const showComputersDrawer = ref(false);
const showComputerRegisterDialog = ref(false);
const selectedLocation = ref<LocationDto | null>(null);

const loading = computed(() => locationStore.loading);
const sortedLocations = computed(() => locationStore.sortedLocations);
const pagination = computed(() => locationStore.pagination);
const computers = computed(() => locationStore.computers);
const computersLoading = computed(() => locationStore.computersLoading);

const sort = ref({
  prop: "sortOrder",
  order: "ascending" as "ascending" | "descending",
});

onMounted(async () => {
  await loadLocations();
});

async function loadLocations() {
  try {
    await locationStore.fetchLocations(
      electionGuid,
      pagination.value.pageNumber,
      pagination.value.pageSize,
    );
  } catch (error) {
    showErrorMessage(
      t("locations.error.failedToLoadLocations") +
        (error instanceof Error ? error.message : ""),
    );
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
      t("locations.confirm.deleteLocationMessage", { name: location.name }),
      t("locations.confirm.deleteLocationTitle"),
      {
        confirmButtonText: t("locations.confirm.delete"),
        cancelButtonText: t("locations.confirm.cancel"),
        type: "warning",
      },
    );

    await locationStore.deleteLocation(electionGuid, location.locationGuid);
    showSuccessMessage(t("locations.success.locationDeleted"));
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage(
        error.message || t("locations.error.failedToDeleteLocation"),
      );
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
  if (!coord) {
    return "";
  }
  const num = Number.parseFloat(coord);
  return Number.isNaN(num) ? coord : num.toFixed(4);
}

function getStatusType(status: string) {
  const typeMap: Record<string, any> = {
    NotStarted: "",
    InProgress: "warning",
    Complete: "success",
    Verified: "info",
  };
  return typeMap[status] || "info";
}

async function viewComputers(location: LocationDto) {
  selectedLocation.value = location;
  showComputersDrawer.value = true;
  try {
    await locationStore.fetchComputers(electionGuid, location.locationGuid);
  } catch (error: any) {
    showErrorMessage(
      t("locations.error.failedToLoadComputers") + (error.message || ""),
    );
  }
}

function openRegisterComputerDialog() {
  showComputerRegisterDialog.value = true;
}

function handleComputerRegistered() {
  showComputerRegisterDialog.value = false;
  if (selectedLocation.value) {
    locationStore.fetchComputers(
      electionGuid,
      selectedLocation.value.locationGuid,
    );
  }
}

async function deleteComputer(computer: ComputerDto) {
  if (!selectedLocation.value) {
    return;
  }

  try {
    await ElMessageBox.confirm(
      t("locations.confirm.deleteComputerMessage", {
        code: computer.computerCode,
      }),
      t("locations.confirm.deleteComputerTitle"),
      {
        confirmButtonText: t("locations.confirm.delete"),
        cancelButtonText: t("locations.confirm.cancel"),
        type: "warning",
      },
    );

    await locationStore.deleteComputer(
      electionGuid,
      selectedLocation.value.locationGuid,
      computer.computerGuid,
    );
    showSuccessMessage(t("locations.success.computerDeleted"));
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage(
        error.message || t("locations.error.failedToDeleteComputer"),
      );
    }
  }
}

function formatDateTime(dateStr?: string): string {
  if (!dateStr) {
    return "-";
  }
  const date = new Date(dateStr);
  return date.toLocaleString();
}
</script>

<template>
  <div class="locations-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-page-header
              :content="t('locations.page.title')"
              @back="goBack"
            />
          </div>
          <div class="header-actions">
            <el-button type="primary" @click="showCreateDialog = true">
              <el-icon><Plus /></el-icon>
              {{ t("locations.button.addLocation") }}
            </el-button>
          </div>
        </div>
      </template>

      <div class="table-container">
        <el-table
          v-loading="loading"
          :data="sortedLocations"
          style="width: 100%"
          @sort-change="handleSortChange"
        >
          <el-table-column
            prop="name"
            :label="$t('locations.form.name')"
            min-width="200"
            sortable="custom"
          />
          <el-table-column
            prop="contactInfo"
            :label="$t('locations.form.contactInfo')"
            min-width="200"
          />
          <el-table-column
            prop="locationTallyStatus"
            :label="$t('locations.tallyStatus')"
            width="150"
          >
            <template #default="scope">
              <el-tag
                v-if="scope.row.locationTallyStatus"
                :type="getStatusType(scope.row.locationTallyStatus)"
              >
                {{ t(`locations.status.${scope.row.locationTallyStatus}`) }}
              </el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column
            prop="ballotsCollected"
            :label="$t('locations.form.ballots')"
            width="100"
            align="center"
            sortable="custom"
          >
            <template #default="scope">
              {{ scope.row.ballotsCollected || 0 }}
            </template>
          </el-table-column>
          <el-table-column
            prop="sortOrder"
            :label="$t('locations.form.sortOrder')"
            width="120"
            align="center"
            sortable="custom"
          >
            <template #default="scope">
              {{ scope.row.sortOrder ?? "-" }}
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('locations.form.coordinates')"
            width="180"
          >
            <template #default="scope">
              <span
                v-if="scope.row.longitude && scope.row.latitude"
                class="coordinates"
              >
                {{ formatCoordinate(scope.row.longitude) }},
                {{ formatCoordinate(scope.row.latitude) }}
              </span>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('locations.form.actions')"
            width="280"
            fixed="right"
          >
            <template #default="scope">
              <el-button-group>
                <el-button size="small" @click="viewComputers(scope.row)">
                  <el-icon><Monitor /></el-icon>
                  {{ $t("locations.form.computers") }}
                </el-button>
                <el-button size="small" @click="editLocation(scope.row)">
                  <el-icon><Edit /></el-icon>
                  {{ $t("locations.form.edit") }}
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  @click="deleteLocation(scope.row)"
                >
                  <el-icon><Delete /></el-icon>
                  {{ $t("locations.form.delete") }}
                </el-button>
              </el-button-group>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="pagination.totalPages > 1" class="pagination-container">
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

    <el-drawer
      v-model="showComputersDrawer"
      :title="`${t('locations.form.computers')} - ${selectedLocation?.name}`"
      size="50%"
      direction="rtl"
    >
      <div class="computers-drawer">
        <div class="drawer-header">
          <el-button type="primary" @click="openRegisterComputerDialog">
            <el-icon><Plus /></el-icon>
            {{ $t("locations.form.registerComputer") }}
          </el-button>
        </div>

        <el-table
          v-loading="computersLoading"
          :data="computers"
          style="width: 100%"
          class="computers-table"
        >
          <el-table-column
            :label="$t('locations.form.computerCode')"
            prop="computerCode"
            width="80"
          />
          <el-table-column
            :label="$t('locations.form.browserInfo')"
            prop="browserInfo"
            min-width="200"
          >
            <template #default="scope">
              <span class="browser-info">{{
                scope.row.browserInfo || "-"
              }}</span>
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('locations.form.ipAddress')"
            prop="ipAddress"
            width="150"
          >
            <template #default="scope">
              {{ scope.row.ipAddress || "-" }}
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('locations.form.lastActivity')"
            prop="lastActivity"
            width="180"
          >
            <template #default="scope">
              {{ formatDateTime(scope.row.lastActivity) }}
            </template>
          </el-table-column>
          <el-table-column :label="$t('locations.form.status')" width="100">
            <template #default="scope">
              <el-tag :type="scope.row.isActive ? 'success' : 'info'">
                {{
                  scope.row.isActive
                    ? $t("locations.form.active")
                    : $t("locations.form.inactive")
                }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column
            :label="$t('locations.form.actions')"
            width="100"
            fixed="right"
          >
            <template #default="scope">
              <el-button
                size="small"
                type="danger"
                @click="deleteComputer(scope.row)"
              >
                <el-icon><Delete /></el-icon>
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <el-empty
          v-if="!computersLoading && computers.length === 0"
          :description="$t('locations.form.noComputersRegistered')"
        />
      </div>
    </el-drawer>

    <ComputerRegistrationDialog
      v-if="selectedLocation"
      v-model="showComputerRegisterDialog"
      :election-guid="electionGuid"
      :location-guid="selectedLocation.locationGuid"
      @success="handleComputerRegistered"
    />
  </div>
</template>

<style lang="less">
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

.computers-drawer {
  padding: 0;
}

.drawer-header {
  margin-bottom: 20px;
}

.computers-table {
  margin-top: 20px;
}

.browser-info {
  font-size: 0.85em;
  word-break: break-word;
}
</style>
