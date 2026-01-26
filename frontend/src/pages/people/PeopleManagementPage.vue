<template>
  <div class="people-management-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-left">
            <el-page-header @back="goBack" :content="$t('people.management')" />
          </div>
          <div class="header-actions">
            <el-input
              v-model="searchQuery"
              :placeholder="$t('people.search')"
              style="width: 250px; margin-right: 10px;"
              clearable
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>
            <el-button type="primary" @click="showAddDialog = true">
              <el-icon><Plus /></el-icon>
              {{ $t('people.addPerson') }}
            </el-button>
          </div>
        </div>
      </template>

      <el-tabs v-model="activeTab" @tab-change="handleTabChange">
        <el-tab-pane :label="$t('people.allPeople')" name="all">
          <PeopleTable
            :people="filteredPeople"
            :loading="loading"
            @edit="handleEdit"
            @delete="handleDelete"
          />
        </el-tab-pane>
        <el-tab-pane :label="$t('people.voters')" name="voters">
          <PeopleTable
            :people="filteredVoters"
            :loading="loading"
            @edit="handleEdit"
            @delete="handleDelete"
          />
        </el-tab-pane>
        <el-tab-pane :label="$t('people.candidates')" name="candidates">
          <PeopleTable
            :people="filteredCandidates"
            :loading="loading"
            @edit="handleEdit"
            @delete="handleDelete"
          />
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <PersonFormDialog
      v-model="showAddDialog"
      :election-guid="electionGuid"
      @success="handleFormSuccess"
    />

    <PersonFormDialog
      v-model="showEditDialog"
      :election-guid="electionGuid"
      :person="editingPerson"
      :is-edit="true"
      @success="handleFormSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Search, Plus } from '@element-plus/icons-vue';
import { usePeopleStore } from '../../stores/peopleStore';
import type { PersonDto } from '../../types';
import PeopleTable from '../../components/people/PeopleTable.vue';
import PersonFormDialog from '../../components/people/PersonFormDialog.vue';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const peopleStore = usePeopleStore();

const electionGuid = route.params.id as string;
const searchQuery = ref('');
const activeTab = ref('all');
const showAddDialog = ref(false);
const showEditDialog = ref(false);
const editingPerson = ref<PersonDto | null>(null);

const loading = computed(() => peopleStore.loading);
const allPeople = computed(() => peopleStore.people);
const voters = computed(() => peopleStore.voters);
const candidates = computed(() => peopleStore.candidates);

const filteredPeople = computed(() => {
  if (!searchQuery.value) return allPeople.value;
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
</style>
