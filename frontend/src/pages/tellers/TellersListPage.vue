<script setup lang="ts">
import TellerFormDialog from "@/components/tellers/TellerFormDialog.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { useTellerStore } from "@/stores/tellerStore";
import type { Teller } from "@/types/teller";
import { Delete, Edit, Plus } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

const router = useRouter();
const route = useRoute();
const tellerStore = useTellerStore();
const { showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const electionGuid = route.params.id as string;
const showCreateDialog = ref(false);
const showEditDialog = ref(false);
const editingTeller = ref<Teller | null>(null);

const loading = computed(() => tellerStore.loading);
const tellers = computed(() => tellerStore.tellers);
const totalCount = computed(() => tellerStore.totalCount);
const currentPage = computed({
  get: () => tellerStore.currentPage,
  set: (val) => {
    tellerStore.currentPage = val;
  },
});
const pageSize = computed({
  get: () => tellerStore.pageSize,
  set: (val) => {
    tellerStore.pageSize = val;
  },
});
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value));

onMounted(async () => {
  await loadTellers();
});

async function loadTellers() {
  try {
    await tellerStore.fetchTellers(
      electionGuid,
      currentPage.value,
      pageSize.value,
    );
  } catch (error) {
    handleApiError(error);
  }
}

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function editTeller(teller: Teller) {
  editingTeller.value = teller;
  showEditDialog.value = true;
}

async function deleteTeller(teller: Teller) {
  try {
    await ElMessageBox.confirm(
      `Are you sure you want to delete teller "${teller.name}"?`,
      "Warning",
      {
        confirmButtonText: "Delete",
        cancelButtonText: "Cancel",
        type: "warning",
      },
    );

    await tellerStore.deleteTeller(electionGuid, teller.rowId);
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage(error.message || "Failed to delete teller");
    }
  }
}

function handleFormSuccess() {
  showCreateDialog.value = false;
  showEditDialog.value = false;
  editingTeller.value = null;
  loadTellers();
}

async function handleSizeChange() {
  await loadTellers();
}

async function handlePageChange() {
  await loadTellers();
}
</script>

<template>
  <div class="tellers-list-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-page-header content="Tellers" @back="goBack" />
          </div>
          <div class="header-actions">
            <el-button type="primary" @click="showCreateDialog = true">
              <el-icon><Plus /></el-icon>
              Add Teller
            </el-button>
          </div>
        </div>
      </template>

      <div class="table-container">
        <el-table v-loading="loading" :data="tellers" style="width: 100%">
          <el-table-column prop="name" label="Teller Name" min-width="200" />
          <el-table-column
            prop="usingComputerCode"
            label="Computer Code"
            width="150"
            align="center"
          >
            <template #default="scope">
              <el-tag v-if="scope.row.usingComputerCode" type="info">
                {{ scope.row.usingComputerCode }}
              </el-tag>
              <span v-else>-</span>
            </template>
          </el-table-column>
          <el-table-column
            prop="isHeadTeller"
            label="Head Teller"
            width="150"
            align="center"
          >
            <template #default="scope">
              <el-tag :type="scope.row.isHeadTeller ? 'success' : 'info'">
                {{ scope.row.isHeadTeller ? "Yes" : "No" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="200" fixed="right">
            <template #default="scope">
              <el-button-group>
                <el-button size="small" @click="editTeller(scope.row)">
                  <el-icon><Edit /></el-icon>
                  Edit
                </el-button>
                <el-button
                  size="small"
                  type="danger"
                  @click="deleteTeller(scope.row)"
                >
                  <el-icon><Delete /></el-icon>
                  Delete
                </el-button>
              </el-button-group>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="totalPages > 1" class="pagination-container">
          <el-pagination
            v-model:current-page="currentPage"
            v-model:page-size="pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="totalCount"
            layout="total, sizes, prev, pager, next"
            @size-change="handleSizeChange"
            @current-change="handlePageChange"
          />
        </div>
      </div>
    </el-card>

    <TellerFormDialog
      v-model="showCreateDialog"
      :election-guid="electionGuid"
      @success="handleFormSuccess"
    />

    <TellerFormDialog
      v-model="showEditDialog"
      :election-guid="electionGuid"
      :teller="editingTeller"
      :is-edit="true"
      @success="handleFormSuccess"
    />
  </div>
</template>

<style lang="less">
.tellers-list-page {
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
</style>
