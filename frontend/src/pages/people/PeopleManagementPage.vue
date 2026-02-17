<template>
  <div class="people-management-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-page-header @back="goBack" :content="$t('people.management')" />
          </div>
          <div class="header-actions">
            <el-space>
              <el-input v-model="searchQuery" :placeholder="$t('people.search')" style="width: 250px;" clearable
                @input="handleSearch">
                <template #prefix>
                  <el-icon>
                    <Search />
                  </el-icon>
                </template>
              </el-input>
              <el-button type="primary" @click="showAddDialog = true">
                <el-icon>
                  <Plus />
                </el-icon>
                {{ $t('people.addPerson') }}
              </el-button>
              <el-dropdown @command="handleBulkAction">
                <el-button type="default">
                  <el-icon>
                    <MoreFilled />
                  </el-icon>
                  {{ $t('people.bulkActions') }}
                  <el-icon class="el-icon--right">
                    <ArrowDown />
                  </el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="import">
                      <el-icon>
                        <Upload />
                      </el-icon>
                      {{ $t('people.importPeople') }}
                    </el-dropdown-item>
                    <el-dropdown-item command="export">
                      <el-icon>
                        <Download />
                      </el-icon>
                      {{ $t('people.exportPeople') }}
                    </el-dropdown-item>
                    <el-dropdown-item command="delete" :disabled="selectedPeople.length === 0" class="danger-item">
                      <el-icon>
                        <Delete />
                      </el-icon>
                      {{ $t('people.deleteSelected', { count: selectedPeople.length }) }}
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </el-space>
          </div>
        </div>
      </template>

      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane :label="$t('people.allPeople')" name="all">
          <PeopleTable :people="filteredPeople" :loading="loading" :show-selection="true" :selected="selectedPeople"
            @edit="handleEdit" @delete="handleDelete" @selection-change="handleSelectionChange" />
        </el-tab-pane>
        <el-tab-pane :label="$t('people.voters')" name="voters">
          <PeopleTable :people="filteredVoters" :loading="loading" :show-selection="true" :selected="selectedPeople"
            @edit="handleEdit" @delete="handleDelete" @selection-change="handleSelectionChange" />
        </el-tab-pane>
        <el-tab-pane :label="$t('people.candidates')" name="candidates">
          <PeopleTable :people="filteredCandidates" :loading="loading" :show-selection="true" :selected="selectedPeople"
            @edit="handleEdit" @delete="handleDelete" @selection-change="handleSelectionChange" />
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <PersonFormDialog v-model="showAddDialog" :election-guid="electionGuid" @success="handleFormSuccess" />

    <PersonFormDialog v-model="showEditDialog" :election-guid="electionGuid" :person="editingPerson" :is-edit="true"
      @success="handleFormSuccess" />



    <!-- Bulk Delete Confirmation -->
    <el-dialog v-model="showBulkDeleteConfirm" :title="$t('people.confirmBulkDelete')" width="500px">
      <p>{{ $t('people.bulkDeleteMessage', { count: selectedPeople.length }) }}</p>
      <p class="warning-text">{{ $t('common.actionIrreversible') }}</p>

      <template #footer>
        <el-button @click="showBulkDeleteConfirm = false">
          {{ $t('common.cancel') }}
        </el-button>
        <el-button type="danger" @click="confirmBulkDelete" :loading="bulkDeleting">
          {{ $t('common.delete') }}
        </el-button>
      </template>
    </el-dialog>


  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, Plus, MoreFilled, ArrowDown, Download, Delete } from '@element-plus/icons-vue';
import { usePeopleStore } from '../../stores/peopleStore';
import { useImportStore } from '../../stores/importStore';
import { useAuthStore } from '../../stores/authStore';
import type { PersonDto } from '../../types';
import PeopleTable from '../../components/people/PeopleTable.vue';
import PersonFormDialog from '../../components/people/PersonFormDialog.vue';


const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const peopleStore = usePeopleStore();
const importStore = useImportStore();
const authStore = useAuthStore();

const electionGuid = route.params.id as string;
const searchQuery = ref('');
const activeTab = ref('all');
const showAddDialog = ref(false);
const showEditDialog = ref(false);
const editingPerson = ref<PersonDto | null>(null);

// Bulk operations
const selectedPeople = ref<PersonDto[]>([]);
const showBulkDeleteConfirm = ref(false);
const bulkDeleting = ref(false);

// Import/Export

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

// No need for Authorization header - credentials: 'include' sends httpOnly cookies automatically
const uploadHeaders = computed(() => ({
  // Authorization header not needed with httpOnly cookies
}));

const loading = computed(() => peopleStore.loading);
const allPeople = computed(() => peopleStore.people);
const voters = computed(() => peopleStore.voters);
const candidates = computed(() => peopleStore.candidates);

const filteredPeople = computed(() => {
  if (!searchQuery.value) {
    return allPeople.value;
  }
  const query = searchQuery.value.toLowerCase();
  return allPeople.value.filter(p =>
    p.fullName.toLowerCase().includes(query) ||
    p.email?.toLowerCase().includes(query) ||
    p.bahaiId?.toLowerCase().includes(query)
  );
});

const filteredVoters = computed(() => {
  if (!searchQuery.value) return voters.value;
  const query = searchQuery.value.toLowerCase();
  return voters.value.filter(p =>
    p.fullName.toLowerCase().includes(query) ||
    p.email?.toLowerCase().includes(query)
  );
});

const filteredCandidates = computed(() => {
  if (!searchQuery.value) return candidates.value;
  const query = searchQuery.value.toLowerCase();
  return candidates.value.filter(p =>
    p.fullName.toLowerCase().includes(query) ||
    p.email?.toLowerCase().includes(query)
  );
});

onMounted(async () => {
  try {
    await peopleStore.initializeSignalR();
    await peopleStore.joinElection(electionGuid);
    await peopleStore.fetchPeople(electionGuid);
  } catch (error) {
    ElMessage.error(t('people.loadError'));
  }
});

onUnmounted(async () => {
  try {
    await peopleStore.leaveElection(electionGuid);
  } catch (error) {
    console.error('Failed to leave election group:', error);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function handleTabChange() {
  searchQuery.value = '';
  selectedPeople.value = []; // Clear selection when changing tabs
}

function handleSearch() {
  selectedPeople.value = []; // Clear selection when searching
}

function handleEdit(person: PersonDto) {
  editingPerson.value = person;
  showEditDialog.value = true;
}

async function handleDelete(person: PersonDto) {
  try {
    await ElMessageBox.confirm(
      t('people.deleteConfirm', { name: person.fullName }),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning'
      }
    );

    await peopleStore.deletePerson(person.personGuid);
    ElMessage.success(t('people.deleteSuccess'));
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || t('people.deleteError'));
    }
  }
}

function handleFormSuccess() {
  showAddDialog.value = false;
  showEditDialog.value = false;
  editingPerson.value = null;
}

function handleSelectionChange(selection: PersonDto[]) {
  selectedPeople.value = selection;
}

function handleBulkAction(command: string) {
  switch (command) {
    case 'import':
      router.push(`/elections/${electionGuid}/people/import`);
      break;
    case 'export':
      handleExport();
      break;
    case 'delete':
      if (selectedPeople.value.length > 0) {
        showBulkDeleteConfirm.value = true;
      }
      break;
  }
}

async function handleExport() {
  try {
    // Create a download link for the export API
    const exportUrl = `${apiBaseUrl}/elections/${electionGuid}/people/export`;
    const link = document.createElement('a');
    link.href = exportUrl;
    link.setAttribute('download', `people-${electionGuid}.xlsx`);
    link.style.display = 'none';

    // Use XMLHttpRequest with credentials to send httpOnly cookies
    const xhr = new XMLHttpRequest();
    xhr.open('GET', exportUrl);
    xhr.withCredentials = true; // Send httpOnly cookies automatically
    xhr.responseType = 'blob';

    xhr.onload = function () {
      if (xhr.status === 200) {
        const blob = new Blob([xhr.response], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const url = window.URL.createObjectURL(blob);
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        ElMessage.success(t('people.exportSuccess'));
      } else {
        ElMessage.error(t('people.exportError'));
      }
    };

    xhr.onerror = function () {
      ElMessage.error(t('people.exportError'));
    };

    xhr.send();
  } catch (error) {
    ElMessage.error(t('people.exportError'));
  }
}

async function confirmBulkDelete() {
  if (selectedPeople.value.length === 0) return;

  bulkDeleting.value = true;
  try {
    const deletePromises = selectedPeople.value.map(person =>
      peopleStore.deletePerson(person.personGuid)
    );

    await Promise.all(deletePromises);
    ElMessage.success(t('people.bulkDeleteSuccess', { count: selectedPeople.value.length }));
    selectedPeople.value = [];
    showBulkDeleteConfirm.value = false;
  } catch (error: any) {
    ElMessage.error(error.message || t('people.bulkDeleteError'));
  } finally {
    bulkDeleting.value = false;
  }
}


</script>

<style scoped>
.people-management-page {
  max-width: 1400px;
  margin: 0 auto;
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
  align-items: center;
}

.import-instructions {
  margin-bottom: var(--spacing-4);
}

.import-instructions h4 {
  margin: 0 0 var(--spacing-2) 0;
  color: var(--color-text-primary);
}

.import-instructions ul {
  margin: var(--spacing-2) 0;
  padding-left: var(--spacing-4);
}

.import-instructions li {
  margin-bottom: var(--spacing-1);
}

.warning-text {
  color: var(--color-error-600);
  font-weight: var(--font-weight-medium);
  margin: var(--spacing-2) 0 0 0;
}

.danger-item {
  color: var(--color-error-600);
}

.danger-item:hover {
  background-color: var(--color-error-50);
}
</style>
