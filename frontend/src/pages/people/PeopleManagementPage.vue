<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { useViewportTableHeight } from "@/composables/useViewportTableHeight";
import { Plus, Search, Upload } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, onUnmounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import PeopleTable from "../../components/people/PeopleTable.vue";
import PersonForm from "../../components/people/PersonForm.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { peopleImportService } from "@/services/peopleImportService";
import { peopleService } from "@/services/peopleService";
import { usePeopleStore } from "../../stores/peopleStore";
import type {
  CheckSelectedWhatsAppResultDto,
  PersonListDto,
  WhatsAppNotifyStatusDto,
} from "../../types";
import {
  canCheckSelectedWhatsApp,
  isWhatsAppCheckAbortError,
  MAX_WHATSAPP_CHECK_SELECTED,
  nextSelectedGuidsAfterWhatsAppCheck,
  selectedPeopleWithPhone,
  whatsAppCheckOutcomeLabel,
} from "@/utils/whatsAppCheckSelected";
import {
  canNotifySelectedWhatsApp,
  notifyCancelledCount,
  whatsAppNotifyOutcomeLabel,
} from "@/utils/whatsAppNotify";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const peopleStore = usePeopleStore();
const { showErrorMessage, showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();
const deletingAll = ref(false);

const electionGuid = route.params.id as string;
const searchQuery = ref("");
const showPersonDrawer = ref(false);
const drawerMode = ref<"add" | "edit">("edit");
const editingPerson = ref<PersonListDto | null>(null);

const peoplePageRef = ref<HTMLElement | null>(null);
const tableWrapperRef = ref<HTMLElement | null>(null);
const { height: tableHeight } = useViewportTableHeight(tableWrapperRef, {
  paddingRootRef: peoplePageRef,
  min: 200,
});

const loading = computed(() => peopleStore.loading);
const allPeople = computed(() => peopleStore.peopleList);
const selectedGuids = ref<string[]>([]);
const checkingWhatsApp = ref(false);
const checkWhatsAppResults = ref<CheckSelectedWhatsAppResultDto | null>(null);
const showCheckWhatsAppResults = ref(false);
let checkWhatsAppAbort: AbortController | null = null;
const notifyingWhatsApp = ref(false);
const notifyWhatsAppResults = ref<WhatsAppNotifyStatusDto | null>(null);
const showNotifyWhatsAppResults = ref(false);
let notifyWhatsAppToken: string | null = null;
let notifyPollTimer: number | null = null;

const filteredPeople = computed(() => {
  if (!searchQuery.value) {
    return allPeople.value;
  }
  const query = searchQuery.value.toLowerCase();
  return allPeople.value.filter(
    (p) =>
      p.fullName?.toLowerCase().includes(query) ||
      p.email?.toLowerCase().includes(query),
  );
});

const selectedWithPhone = computed(() =>
  selectedPeopleWithPhone(allPeople.value, selectedGuids.value),
);
const canCheckWhatsApp = computed(() =>
  canCheckSelectedWhatsApp(
    selectedGuids.value.length,
    selectedWithPhone.value.length,
  ),
);
const canNotifyWhatsApp = computed(() =>
  canNotifySelectedWhatsApp(
    selectedGuids.value.length,
    selectedWithPhone.value.length,
  ),
);

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
  stopNotifyPoll();
  try {
    await peopleStore.leaveElection(electionGuid);
  } catch (error) {
    console.error(t("people.leaveElectionGroupError"), error);
  }
});

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

function resultPersonName(personGuid: string): string {
  return (
    allPeople.value.find((person) => person.personGuid === personGuid)
      ?.fullName ?? personGuid
  );
}

function cancelCheckWhatsAppSelected() {
  checkWhatsAppAbort?.abort();
}

async function handleCheckWhatsAppSelected() {
  if (!canCheckWhatsApp.value) {
    if (selectedGuids.value.length > MAX_WHATSAPP_CHECK_SELECTED) {
      showErrorMessage(
        t("people.phoneOnlineVoter.whatsAppTooMany", {
          max: MAX_WHATSAPP_CHECK_SELECTED,
        }),
      );
    } else {
      showErrorMessage(t("people.checkWhatsAppSelectedNone"));
    }
    return;
  }

  checkWhatsAppAbort?.abort();
  checkWhatsAppAbort = new AbortController();
  checkingWhatsApp.value = true;
  try {
    const result = await peopleService.checkWhatsAppSelected(
      electionGuid,
      selectedGuids.value,
      checkWhatsAppAbort.signal,
    );
    checkWhatsAppResults.value = result;
    showCheckWhatsAppResults.value = true;
    showSuccessMessage(
      t("people.checkWhatsAppSelectedSummary", {
        checked: result.checked,
        ok: result.ok,
        noWa: result.noWa,
        failed: result.failed,
        skipped: result.skipped,
      }),
    );
    await peopleStore.fetchPeopleList(electionGuid);
    selectedGuids.value = nextSelectedGuidsAfterWhatsAppCheck(
      selectedGuids.value,
      "completed",
    );
  } catch (error) {
    if (isWhatsAppCheckAbortError(error)) {
      // Cancel aborts fetch — no response body / result.cancelled dialog.
      showErrorMessage(t("people.checkWhatsAppSelectedCancelled"));
      await peopleStore.fetchPeopleList(electionGuid);
      selectedGuids.value = nextSelectedGuidsAfterWhatsAppCheck(
        selectedGuids.value,
        "aborted",
      );
    } else {
      handleApiError(error);
    }
  } finally {
    checkingWhatsApp.value = false;
    checkWhatsAppAbort = null;
  }
}

function stopNotifyPoll() {
  if (notifyPollTimer !== null) {
    window.clearInterval(notifyPollTimer);
    notifyPollTimer = null;
  }
}

function applyNotifyStatus(status: WhatsAppNotifyStatusDto) {
  notifyWhatsAppResults.value = status;
  notifyWhatsAppToken = status.queueToken;
  if (!status.running) {
    stopNotifyPoll();
    notifyingWhatsApp.value = false;
  }
}

async function pollNotifyStatus() {
  if (!notifyWhatsAppToken) {
    return;
  }
  try {
    const status = await peopleService.getWhatsAppNotifyStatus(
      electionGuid,
      notifyWhatsAppToken,
    );
    applyNotifyStatus(status);
  } catch (error) {
    stopNotifyPoll();
    notifyingWhatsApp.value = false;
    handleApiError(error);
  }
}

async function handleNotifyWhatsApp() {
  if (!canNotifyWhatsApp.value) {
    if (selectedGuids.value.length > MAX_WHATSAPP_CHECK_SELECTED) {
      showErrorMessage(
        t("people.phoneOnlineVoter.whatsAppTooMany", {
          max: MAX_WHATSAPP_CHECK_SELECTED,
        }),
      );
    } else {
      showErrorMessage(t("people.notifyWhatsAppNone"));
    }
    return;
  }

  notifyingWhatsApp.value = true;
  showNotifyWhatsAppResults.value = true;
  try {
    const status = await peopleService.startWhatsAppNotify(
      electionGuid,
      selectedGuids.value,
    );
    applyNotifyStatus(status);
    if (status.running) {
      showSuccessMessage(
        t("people.notifyWhatsAppQueued", { queued: status.queued }),
      );
      stopNotifyPoll();
      notifyPollTimer = window.setInterval(() => {
        void pollNotifyStatus();
      }, 2000);
    }
  } catch (error) {
    notifyingWhatsApp.value = false;
    handleApiError(error);
  }
}

async function handleAbortWhatsAppNotify() {
  if (!notifyWhatsAppToken) {
    return;
  }
  try {
    const status = await peopleService.abortWhatsAppNotify(
      electionGuid,
      notifyWhatsAppToken,
    );
    applyNotifyStatus(status);
    await pollNotifyStatus();
  } catch (error) {
    handleApiError(error);
  }
}

async function handleDeleteAllPeople() {
  try {
    await ElMessageBox.confirm(
      t("people.import.deleteAllPeopleMessage"),
      t("people.import.confirmDeleteAllPeople"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );
  } catch {
    return;
  }

  deletingAll.value = true;
  try {
    const result = await peopleImportService.deleteAllPeople(electionGuid);
    showSuccessMessage(
      t("people.import.deleteAllSuccess", { count: result.deletedCount }),
    );
    await peopleStore.fetchPeopleList(electionGuid);
  } catch (error) {
    handleApiError(error);
  } finally {
    deletingAll.value = false;
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
              <el-button
                type="default"
                :disabled="!canCheckWhatsApp || checkingWhatsApp"
                :loading="checkingWhatsApp"
                @click="handleCheckWhatsAppSelected"
              >
                {{
                  $t("people.checkWhatsAppSelected", {
                    count: selectedWithPhone.length,
                  })
                }}
              </el-button>
              <el-button
                v-if="checkingWhatsApp"
                @click="cancelCheckWhatsAppSelected"
              >
                {{ $t("people.checkWhatsAppSelectedCancel") }}
              </el-button>
              <el-button
                type="default"
                :disabled="!canNotifyWhatsApp || notifyingWhatsApp"
                :loading="notifyingWhatsApp"
                @click="handleNotifyWhatsApp"
              >
                {{
                  $t("people.notifyWhatsApp", {
                    count: selectedWithPhone.length,
                  })
                }}
              </el-button>
              <el-button
                v-if="notifyingWhatsApp"
                @click="handleAbortWhatsAppNotify"
              >
                {{ $t("people.notifyWhatsAppAbort") }}
              </el-button>
              <span v-if="selectedGuids.length" class="selected-count">
                {{
                  $t("people.selectedCount", { count: selectedGuids.length })
                }}
              </span>
            </el-space>
          </div>
          <el-button
            type="danger"
            plain
            :loading="deletingAll"
            :disabled="allPeople.length === 0"
            @click="handleDeleteAllPeople"
          >
            {{ $t("people.import.deleteAllPeople") }}
          </el-button>
        </div>
      </template>

      <div ref="tableWrapperRef" class="people-table-wrapper">
        <PeopleTable
          v-model:selected-guids="selectedGuids"
          :people="filteredPeople"
          :loading="loading"
          :table-height="tableHeight"
          @edit="handleEdit"
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
      v-model="showCheckWhatsAppResults"
      :title="$t('people.checkWhatsAppSelectedResults')"
      width="520px"
    >
      <p v-if="checkWhatsAppResults">
        {{
          $t("people.checkWhatsAppSelectedSummary", {
            checked: checkWhatsAppResults.checked,
            ok: checkWhatsAppResults.ok,
            noWa: checkWhatsAppResults.noWa,
            failed: checkWhatsAppResults.failed,
            skipped: checkWhatsAppResults.skipped,
          })
        }}
      </p>
      <ul v-if="checkWhatsAppResults" class="whatsapp-check-results">
        <li v-for="row in checkWhatsAppResults.results" :key="row.personGuid">
          {{ resultPersonName(row.personGuid) }} —
          {{ whatsAppCheckOutcomeLabel(row.outcome, t) }}
        </li>
      </ul>
      <template #footer>
        <el-button @click="showCheckWhatsAppResults = false">
          {{ $t("people.checkWhatsAppSelectedClose") }}
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="showNotifyWhatsAppResults"
      :title="$t('people.notifyWhatsAppResults')"
      width="520px"
    >
      <p v-if="notifyWhatsAppResults">
        {{
          $t("people.notifyWhatsAppSummary", {
            sent: notifyWhatsAppResults.sent,
            skipped: notifyWhatsAppResults.skipped,
            failed: notifyWhatsAppResults.failed,
            cancelled: notifyCancelledCount(notifyWhatsAppResults.results),
          })
        }}
      </p>
      <p v-if="notifyWhatsAppResults?.running">
        {{
          $t("people.notifyWhatsAppQueued", {
            queued: notifyWhatsAppResults.queued,
          })
        }}
      </p>
      <ul v-if="notifyWhatsAppResults" class="whatsapp-check-results">
        <li v-for="row in notifyWhatsAppResults.results" :key="row.personGuid">
          {{ resultPersonName(row.personGuid) }} —
          {{ whatsAppNotifyOutcomeLabel(row.outcome, t) }}
        </li>
      </ul>
      <template #footer>
        <el-button v-if="notifyingWhatsApp" @click="handleAbortWhatsAppNotify">
          {{ $t("people.notifyWhatsAppAbort") }}
        </el-button>
        <el-button @click="showNotifyWhatsAppResults = false">
          {{ $t("people.notifyWhatsAppClose") }}
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

  .selected-count {
    color: var(--el-text-color-secondary);
    font-size: var(--el-font-size-small);
  }
}

.whatsapp-check-results {
  margin: 12px 0 0;
  padding-left: 20px;
  max-height: 240px;
  overflow: auto;
}

.person-form-drawer {
  .el-drawer {
    transition: none;
  }
}
</style>
