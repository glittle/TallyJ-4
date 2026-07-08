<script setup lang="ts">
import TellerForm from "@/components/tellers/TellerForm.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useTellerStore } from "@/stores/tellerStore";
import type { Teller } from "@/types/teller";
import { Plus } from "@element-plus/icons-vue";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

const route = useRoute();
const tellerStore = useTellerStore();
const { handleApiError } = useApiErrorHandler();
const { t } = useI18n();

const electionGuid = route.params.id as string;
const showTellerDrawer = ref(false);
const drawerMode = ref<"add" | "edit">("edit");
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

const tellerDrawerTitle = computed(() => {
  if (drawerMode.value === "add") {
    return t("teller.form.titleAdd");
  }
  if (!editingTeller.value) {
    return t("teller.form.titleEdit");
  }
  return t("teller.editDrawerTitle", { name: editingTeller.value.name });
});

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

function handleAdd() {
  drawerMode.value = "add";
  editingTeller.value = null;
  showTellerDrawer.value = true;
}

function handleEdit(teller: Teller) {
  drawerMode.value = "edit";
  editingTeller.value = teller;
  showTellerDrawer.value = true;
}

function handleTellerDrawerClosed() {
  editingTeller.value = null;
}

function handleFormSuccess() {
  showTellerDrawer.value = false;
  editingTeller.value = null;
  loadTellers();
}

function handleTellerDeleted() {
  showTellerDrawer.value = false;
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
          <div class="header-actions">
            <el-button type="primary" @click="handleAdd">
              <el-icon><Plus /></el-icon>
              {{ $t("teller.form.titleAdd") }}
            </el-button>
          </div>
        </div>
      </template>

      <div class="table-container">
        <el-table v-loading="loading" :data="tellers" style="width: 100%">
          <el-table-column
            prop="name"
            :label="$t('teller.form.name')"
            min-width="200"
          >
            <template #default="scope">
              <el-button type="primary" link @click="handleEdit(scope.row)">
                {{ scope.row.name }}
              </el-button>
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

    <el-drawer
      v-model="showTellerDrawer"
      :title="tellerDrawerTitle"
      direction="rtl"
      size="50%"
      :lock-scroll="false"
      modal-class="teller-form-drawer"
      @closed="handleTellerDrawerClosed"
    >
      <TellerForm
        v-if="showTellerDrawer && (drawerMode === 'add' || editingTeller)"
        :key="
          drawerMode === 'add' ? 'add-teller' : (editingTeller?.rowId ?? 'edit')
        "
        :election-guid="electionGuid"
        :teller="drawerMode === 'edit' ? editingTeller : null"
        :is-edit="drawerMode === 'edit'"
        :show-delete="drawerMode === 'edit'"
        @success="handleFormSuccess"
        @deleted="handleTellerDeleted"
        @cancel="showTellerDrawer = false"
      />
    </el-drawer>
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

.teller-form-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
