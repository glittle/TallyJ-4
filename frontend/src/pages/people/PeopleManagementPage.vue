<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { useViewportTableHeight } from "@/composables/useViewportTableHeight";
import {
  ArrowDown,
  Delete,
  MoreFilled,
  Plus,
  Search,
  Upload,
} from "@element-plus/icons-vue";
import { computed, onMounted, onUnmounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import PeopleTable from "../../components/people/PeopleTable.vue";
import PersonForm from "../../components/people/PersonForm.vue";
import { usePeopleStore } from "../../stores/peopleStore";
import type { PersonListDto } from "../../types";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const peopleStore = usePeopleStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const searchQuery = ref("");
const showPersonDrawer = ref(false);
const drawerMode = ref<"add" | "edit">("edit");
const editingPerson = ref<PersonListDto | null>(null);

const selectedPeople = ref<PersonListDto[]>([]);
const showBulkDeleteConfirm = ref(false);
const bulkDeleting = ref(false);

const peoplePageRef = ref<HTMLElement | null>(null);
const tableWrapperRef = ref<HTMLElement | null>(null);
const { height: tableHeight } = useViewportTableHeight(tableWrapperRef, {
  paddingRootRef: peoplePageRef,
  min: 200,
});

const loading = computed(() => peopleStore.loading);
const allPeople = computed(() => peopleStore.peopleList);

const filteredPeople = computed(() => {
  if (!searchQuery.value) {
    return allPeople.value;
  }
  const query = searchQuery.value.toLowerCase();
  return allPeople.value.filter(
    (p) =>
      p.fullName.toLowerCase().includes(query) ||
      p.email?.toLowerCase().includes(query),
  );
});

const personDrawerTitle = computed(() => {
  if (drawerMode.value === "add") {
    return t("people.addPerson");
  }
  if (!editingPerson.value) {
    return t("people.editPerson");
  }
  return t("people.editDrawerTitle", { name: editingPerson.value.fullName });
});

onMounted(async () => {
  try {
    await peopleStore.initializeSignalR();
    await peopleStore.joinElection(electionGuid);
    await peopleStore.fetchPeopleList(electionGuid);
  } catch (error) {
    showErrorMessage(`${t("people.loadError")} ${error}`);
  }
});

onUnmounted(async () => {
  try {
    await peopleStore.leaveElection(electionGuid);
  } catch (error) {
    console.error(t("people.leaveElectionGroupError"), error);
  }
});

function handleSearch() {
  selectedPeople.value = [];
}

function handleAdd() {
  drawerMode.value = "add";
  editingPerson.value = null;
  showPersonDrawer.value = true;
}

function handleEdit(person: PersonListDto) {
  drawerMode.value = "edit";
  editingPerson.value = person;
  showPersonDrawer.value = true;
}

function handlePersonDrawerClosed() {
  editingPerson.value = null;
}

function handleFormSuccess() {
  showPersonDrawer.value = false;
  editingPerson.value = null;
}

function handlePersonDeleted() {
  showPersonDrawer.value = false;
  editingPerson.value = null;
}

function handleImport() {
  router.push(`/elections/${electionGuid}/people/import`);
}

function handleBulkAction(command: string) {
  switch (command) {
    case "delete":
      if (selectedPeople.value.length > 0) {
        showBulkDeleteConfirm.value = true;
      }
      break;
  }
}

async function confirmBulkDelete() {
  if (selectedPeople.value.length === 0) {
    return;
  }

  bulkDeleting.value = true;
  try {
    const deletePromises = selectedPeople.value.map((person) =>
      peopleStore.deletePerson(person.personGuid),
    );

    await Promise.all(deletePromises);
    showSuccessMessage(
      t("people.bulkDeleteSuccess", { count: selectedPeople.value.length }),
    );
    selectedPeople.value = [];
    showBulkDeleteConfirm.value = false;
  } catch (error: any) {
    showErrorMessage(error.message || t("people.bulkDeleteError"));
  } finally {
    bulkDeleting.value = false;
  }
}
</script>

<template>
  <div ref="peoplePageRef" class="people-management-page">
    <el-card class="people-management-card">
      <template #header>
        <div class="card-header">
          <div class="header-actions">
            <el-space>
              <el-input
                v-model="searchQuery"
                :placeholder="$t('people.search')"
                style="width: 250px"
                clearable
                @input="handleSearch"
              >
                <template #prefix>
                  <el-icon>
                    <Search />
                  </el-icon>
                </template>
              </el-input>
              <el-button type="primary" @click="handleAdd">
                <el-icon>
                  <Plus />
                </el-icon>
                {{ $t("people.addPerson") }}
              </el-button>
              <el-button type="default" @click="handleImport">
                <el-icon>
                  <Upload />
                </el-icon>
                {{ $t("people.importPeople") }}
              </el-button>
              <el-dropdown @command="handleBulkAction">
                <el-button type="default">
                  <el-icon>
                    <MoreFilled />
                  </el-icon>
                  {{ $t("people.bulkActions") }}
                  <el-icon class="el-icon--right">
                    <ArrowDown />
                  </el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item
                      command="delete"
                      :disabled="selectedPeople.length === 0"
                      class="danger-item"
                    >
                      <el-icon>
                        <Delete />
                      </el-icon>
                      {{
                        $t("people.deleteSelected", {
                          count: selectedPeople.length,
                        })
                      }}
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </el-space>
          </div>
        </div>
      </template>

      <div ref="tableWrapperRef" class="people-table-wrapper">
        <PeopleTable
          :people="filteredPeople"
          :loading="loading"
          :table-height="tableHeight"
          :show-selection="true"
          :selected="selectedPeople"
          @edit="handleEdit"
          @selection-change="selectedPeople = $event"
        />
      </div>
    </el-card>

    <el-drawer
      v-model="showPersonDrawer"
      :title="personDrawerTitle"
      direction="rtl"
      size="50%"
      :lock-scroll="false"
      modal-class="person-form-drawer"
      @closed="handlePersonDrawerClosed"
    >
      <PersonForm
        v-if="showPersonDrawer && (drawerMode === 'add' || editingPerson)"
        :key="
          drawerMode === 'add'
            ? 'add-person'
            : (editingPerson?.personGuid ?? 'edit')
        "
        :election-guid="electionGuid"
        :person="drawerMode === 'edit' ? editingPerson : null"
        :is-edit="drawerMode === 'edit'"
        :show-delete="drawerMode === 'edit'"
        @success="handleFormSuccess"
        @deleted="handlePersonDeleted"
        @cancel="showPersonDrawer = false"
      />
    </el-drawer>

    <el-dialog
      v-model="showBulkDeleteConfirm"
      :title="$t('people.confirmBulkDelete')"
      width="500px"
    >
      <p>
        {{ $t("people.bulkDeleteMessage", { count: selectedPeople.length }) }}
      </p>
      <p class="warning-text">{{ $t("common.actionIrreversible") }}</p>

      <template #footer>
        <el-button @click="showBulkDeleteConfirm = false">
          {{ $t("common.cancel") }}
        </el-button>
        <el-button
          type="danger"
          :loading="bulkDeleting"
          @click="confirmBulkDelete"
        >
          {{ $t("common.delete") }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.people-management-page {
  max-width: 1400px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  flex-direction: column;

  .people-management-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;

    .el-card__body {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-height: 0;
    }
  }

  .people-table-wrapper {
    flex: 1;
    min-height: 0;
  }

  .card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
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

.person-form-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
