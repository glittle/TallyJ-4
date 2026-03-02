<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessageBox } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { Search, Plus, MoreFilled, ArrowDown, Delete, Upload } from '@element-plus/icons-vue';
import { usePeopleStore } from '../../stores/peopleStore';
import type { PersonListDto } from '../../types';
import PeopleTable from '../../components/people/PeopleTable.vue';
import PersonFormDialog from '../../components/people/PersonFormDialog.vue';


const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const peopleStore = usePeopleStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const searchQuery = ref('');
const activeTab = ref('all');
const showAddDialog = ref(false);
const showEditDialog = ref(false);
const editingPerson = ref<PersonListDto | null>(null);

// Bulk operations
const selectedPeople = ref<PersonListDto[]>([]);
const showBulkDeleteConfirm = ref(false);
const bulkDeleting = ref(false);

// Export

const loading = computed(() => peopleStore.loading);
const allPeople = computed(() => peopleStore.peopleList);
const voters = computed(() => peopleStore.voters);
const candidates = computed(() => peopleStore.candidates);

const filteredPeople = computed(() => {
  if (!searchQuery.value) {
    return allPeople.value;
  }
  const query = searchQuery.value.toLowerCase();
  return allPeople.value.filter(p =>
    p.fullName.toLowerCase().includes(query) ||
    p.email?.toLowerCase().includes(query)
    // p.bahaiId?.toLowerCase().includes(query)
  );
});

onMounted(async () => {
  try {
    await peopleStore.initializeSignalR();
    await peopleStore.joinElection(electionGuid);
    await peopleStore.fetchPeopleList(electionGuid);
  } catch (error) {
    showErrorMessage(`${t('people.loadError')} ${error}`);
  }
});

onUnmounted(async () => {
  try {
    await peopleStore.leaveElection(electionGuid);
  } catch (error) {
    console.error(t('people.leaveElectionGroupError'), error);
  }
});

function goBack() {
  router.push(`/elections/${electionGuid}`);
}

function handleSearch() {
  selectedPeople.value = []; // Clear selection when searching
}

function handleEdit(person: PersonListDto) {
  editingPerson.value = person;
  showEditDialog.value = true;
}

async function handleDelete(person: PersonListDto) {
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
    showSuccessMessage(t('people.deleteSuccess'));
  } catch (error: any) {
    if (error !== 'cancel') {
      showErrorMessage(error.message || t('people.deleteError'));
    }
  }
}

function handleFormSuccess() {
  showAddDialog.value = false;
  showEditDialog.value = false;
  editingPerson.value = null;
}

function handleImport() {
  router.push(`/elections/${electionGuid}/people/import`);
}

function handleBulkAction(command: string) {
  switch (command) {
    case 'delete':
      if (selectedPeople.value.length > 0) {
        showBulkDeleteConfirm.value = true;
      }
      break;
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
    showSuccessMessage(t('people.bulkDeleteSuccess', { count: selectedPeople.value.length }));
    selectedPeople.value = [];
    showBulkDeleteConfirm.value = false;
  } catch (error: any) {
    showErrorMessage(error.message || t('people.bulkDeleteError'));
  } finally {
    bulkDeleting.value = false;
  }
}


</script>

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
              <el-button type="default" @click="handleImport">
                <el-icon>
                  <Upload />
                </el-icon>
                {{ $t('people.importPeople') }}
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

      <PeopleTable :people="filteredPeople" :loading="loading" :show-selection="true" :selected="selectedPeople"
        @edit="handleEdit" @delete="handleDelete" />
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

<style type="less">
.people-management-page {
  max-width: 1400px;
  margin: 0 auto;

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
}
</style>
