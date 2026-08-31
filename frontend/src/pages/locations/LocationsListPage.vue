<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { Plus } from "@element-plus/icons-vue";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import LocationForm from "../../components/locations/LocationForm.vue";
import { useLocationStore } from "../../stores/locationStore";
import type { LocationDto } from "../../types";
import { formatLocationLabel } from "@/utils/locationDisplay";

const route = useRoute();
const locationStore = useLocationStore();
const { showErrorMessage } = useNotifications();
const { t } = useI18n();

const electionGuid = route.params.id as string;
const showLocationDrawer = ref(false);
const drawerMode = ref<"add" | "edit">("edit");
const editingLocation = ref<LocationDto | null>(null);
const loading = computed(() => locationStore.loading);
const sortedLocations = computed(() => locationStore.sortedLocations);
const pagination = computed(() => locationStore.pagination);

const sort = ref({
  prop: "sortOrder",
  order: "ascending" as "ascending" | "descending",
});

const locationDrawerTitle = computed(() => {
  if (drawerMode.value === "add") {
    return t("locations.form.titleAdd");
  }
  if (!editingLocation.value) {
    return t("locations.form.titleEdit");
  }
  return t("locations.editDrawerTitle", {
    name: formatLocationLabel(t, editingLocation.value),
  });
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

function handleAdd() {
  drawerMode.value = "add";
  editingLocation.value = null;
  showLocationDrawer.value = true;
}

function handleEdit(location: LocationDto) {
  drawerMode.value = "edit";
  editingLocation.value = location;
  showLocationDrawer.value = true;
}

function handleLocationDrawerClosed() {
  editingLocation.value = null;
}

function handleFormSuccess() {
  showLocationDrawer.value = false;
  editingLocation.value = null;
  loadLocations();
}

function handleLocationDeleted() {
  showLocationDrawer.value = false;
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
</script>

<template>
  <div class="locations-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-actions">
            <el-button type="primary" @click="handleAdd">
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
          >
            <template #default="scope">
              <el-button type="primary" link @click="handleEdit(scope.row)">
                {{ formatLocationLabel($t, scope.row) }}
              </el-button>
            </template>
          </el-table-column>
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

    <el-drawer
      v-model="showLocationDrawer"
      :title="locationDrawerTitle"
      direction="rtl"
      size="50%"
      :lock-scroll="false"
      modal-class="location-form-drawer"
      @closed="handleLocationDrawerClosed"
    >
      <LocationForm
        v-if="showLocationDrawer && (drawerMode === 'add' || editingLocation)"
        :key="
          drawerMode === 'add'
            ? 'add-location'
            : (editingLocation?.locationGuid ?? 'edit')
        "
        :election-guid="electionGuid"
        :location="drawerMode === 'edit' ? editingLocation : null"
        :is-edit="drawerMode === 'edit'"
        :show-delete="drawerMode === 'edit'"
        @success="handleFormSuccess"
        @deleted="handleLocationDeleted"
        @cancel="showLocationDrawer = false"
      />
    </el-drawer>
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

.location-form-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
