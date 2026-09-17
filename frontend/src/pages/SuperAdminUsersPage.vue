<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { useDebounceFn } from "@/utils/debounce";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import {
  superAdminService,
  type SuperAdminUser,
  type SuperAdminUserDetail,
} from "../services/superAdminService";

const { t } = useI18n();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const users = ref<SuperAdminUser[]>([]);
const searchText = ref("");
const loading = ref(false);
const currentPage = ref(1);
const pageSize = ref(25);
const totalCount = ref(0);

const drawerVisible = ref(false);
const selected = ref<SuperAdminUserDetail | null>(null);
const editForm = ref({ displayName: "", email: "" });
const saving = ref(false);

const inviteDialogVisible = ref(false);
const issuingInvite = ref(false);
const issuedInvite = ref<{ inviteUrl: string; expiresAt: string } | null>(null);

async function fetchUsers() {
  loading.value = true;
  try {
    const result = await superAdminService.getUsers({
      search: searchText.value || undefined,
      page: currentPage.value,
      pageSize: pageSize.value,
    });
    users.value = result.items;
    totalCount.value = result.totalCount;
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
  } finally {
    loading.value = false;
  }
}

const debouncedSearch = useDebounceFn(() => {
  currentPage.value = 1;
  fetchUsers();
}, 300);

async function openUser(row: SuperAdminUser) {
  try {
    loading.value = true;
    selected.value = await superAdminService.getUserDetail(row.id);
    editForm.value = {
      displayName: selected.value.displayName || "",
      email: selected.value.email || "",
    };
    drawerVisible.value = true;
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
  } finally {
    loading.value = false;
  }
}

async function saveUser() {
  if (!selected.value) {
    return;
  }
  try {
    saving.value = true;
    selected.value = await superAdminService.updateUser(selected.value.id, {
      displayName: editForm.value.displayName,
      email: editForm.value.email,
    });
    showSuccessMessage(t("superAdmin.users.updateSuccess"));
    await fetchUsers();
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
  } finally {
    saving.value = false;
  }
}

function formatWhen(value?: string) {
  if (!value) {
    return "-";
  }
  return new Date(value).toLocaleString();
}

async function issueInvite() {
  issuingInvite.value = true;
  try {
    issuedInvite.value = await superAdminService.createAccountInvite();
    inviteDialogVisible.value = true;
  } catch (err) {
    showErrorMessage(
      extractApiErrorMessage(err) || t("superAdmin.users.issueInviteFailed"),
    );
  } finally {
    issuingInvite.value = false;
  }
}

async function copyInviteLink() {
  if (!issuedInvite.value?.inviteUrl) {
    return;
  }
  try {
    await navigator.clipboard.writeText(issuedInvite.value.inviteUrl);
    showSuccessMessage(t("superAdmin.users.issueInviteCopied"));
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
  }
}

onMounted(fetchUsers);
</script>

<template>
  <div class="super-admin-users-page">
    <div class="page-header">
      <h1>{{ $t("superAdmin.users.title") }}</h1>
      <router-link to="/super-admin" class="back-link">
        {{ $t("superAdmin.title") }}
      </router-link>
    </div>

    <el-card>
      <div class="toolbar">
        <el-input
          v-model="searchText"
          clearable
          :placeholder="$t('superAdmin.users.search')"
          @input="debouncedSearch"
        />
        <el-button
          type="primary"
          :loading="issuingInvite"
          @click="issueInvite"
        >
          {{ $t("superAdmin.users.issueInvite") }}
        </el-button>
      </div>

      <el-table v-loading="loading" :data="users" stripe @row-click="openUser">
        <el-table-column
          prop="email"
          :label="$t('superAdmin.users.email')"
          min-width="200"
        />
        <el-table-column
          prop="displayName"
          :label="$t('superAdmin.users.name')"
          min-width="140"
        />
        <el-table-column
          prop="authMethod"
          :label="$t('superAdmin.users.authMethod')"
          width="120"
        />
        <el-table-column
          :label="$t('superAdmin.users.emailConfirmed')"
          width="130"
        >
          <template #default="{ row }">
            <el-tag
              :type="row.emailConfirmed ? 'success' : 'info'"
              size="small"
            >
              {{ row.emailConfirmed ? $t("common.yes") : $t("common.no") }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column
          prop="pendingEmail"
          :label="$t('superAdmin.users.pendingEmail')"
          min-width="160"
        />
      </el-table>

      <div v-if="!loading && users.length === 0" class="empty">
        {{ $t("superAdmin.users.noResults") }}
      </div>

      <div class="pager">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          layout="total, prev, pager, next"
          :total="totalCount"
          @current-change="fetchUsers"
        />
      </div>
    </el-card>

    <el-drawer
      v-model="drawerVisible"
      :title="$t('superAdmin.users.editTitle')"
      size="420px"
    >
      <template v-if="selected">
        <el-form label-position="top">
          <el-form-item :label="$t('superAdmin.users.name')">
            <el-input v-model="editForm.displayName" maxlength="200" />
          </el-form-item>
          <el-form-item :label="$t('superAdmin.users.email')">
            <el-input v-model="editForm.email" type="email" />
          </el-form-item>
          <el-button type="primary" :loading="saving" @click="saveUser">
            {{ $t("superAdmin.users.save") }}
          </el-button>
        </el-form>

        <h3 class="history-title">{{ $t("superAdmin.users.emailHistory") }}</h3>
        <el-timeline v-if="selected.emailHistory?.length">
          <el-timeline-item
            v-for="(entry, idx) in selected.emailHistory"
            :key="idx"
            :timestamp="formatWhen(entry.changedAt)"
          >
            <div>
              <strong>{{ $t("superAdmin.users.historyFrom") }}:</strong>
              {{ entry.oldEmail }}
            </div>
            <div>
              <strong>{{ $t("superAdmin.users.historyTo") }}:</strong>
              {{ entry.newEmail }}
            </div>
            <div class="history-source">
              {{ $t("superAdmin.users.historySource") }}: {{ entry.source }}
            </div>
          </el-timeline-item>
        </el-timeline>
        <p v-else class="empty">{{ $t("superAdmin.users.historyEmpty") }}</p>
      </template>
    </el-drawer>

    <el-dialog
      v-model="inviteDialogVisible"
      :title="$t('superAdmin.users.issueInviteTitle')"
      width="520px"
    >
      <p>{{ $t("superAdmin.users.issueInviteHelp") }}</p>
      <el-input
        v-if="issuedInvite"
        :model-value="issuedInvite.inviteUrl"
        readonly
      />
      <p v-if="issuedInvite" class="invite-expiry">
        {{ $t("superAdmin.users.issueInviteExpires") }}:
        {{ formatWhen(issuedInvite.expiresAt) }}
      </p>
      <template #footer>
        <el-button @click="inviteDialogVisible = false">
          {{ $t("common.close") }}
        </el-button>
        <el-button type="primary" @click="copyInviteLink">
          {{ $t("superAdmin.users.issueInviteCopy") }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style lang="less">
.super-admin-users-page {
  max-width: 1100px;
  margin: 0 auto;

  .page-header {
    display: flex;
    align-items: baseline;
    justify-content: space-between;
    margin-bottom: 16px;

    h1 {
      margin: 0;
      font-size: 1.5rem;
    }

    .back-link {
      color: var(--el-color-primary);
      text-decoration: none;
    }
  }

  .toolbar {
    margin-bottom: 16px;
    display: flex;
    gap: 12px;
    align-items: center;
    max-width: 720px;
  }

  .invite-expiry {
    margin: 12px 0 0;
    color: var(--el-text-color-secondary);
  }

  .empty {
    padding: 16px 0;
    color: var(--el-text-color-secondary);
  }

  .pager {
    margin-top: 16px;
    display: flex;
    justify-content: flex-end;
  }

  .history-title {
    margin: 28px 0 12px;
    font-size: 1.05rem;
  }

  .history-source {
    font-size: 0.85rem;
    color: var(--el-text-color-secondary);
  }

  tr {
    cursor: pointer;
  }
}
</style>
