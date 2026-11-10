<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { isGuestTeller } from "@/domain/guestTellerAccess";
import {
  electionAdminService,
  type ElectionAdmin,
} from "@/services/electionAdminService";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { ElMessageBox } from "element-plus";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  electionGuid: string;
}>();

const { t } = useI18n();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const visible = ref(!isGuestTeller());
const loading = ref(false);
const adding = ref(false);
const email = ref("");
const admins = ref<ElectionAdmin[]>([]);
const removingId = ref<string | null>(null);

onMounted(() => {
  if (visible.value) {
    void load();
  }
});

async function load() {
  loading.value = true;
  try {
    admins.value = await electionAdminService.list(props.electionGuid);
  } catch (error: unknown) {
    if (extractApiErrorMessage(error) === "error.forbidden") {
      visible.value = false;
      return;
    }
    handleApiError(error, t("elections.admins.loadError"));
  } finally {
    loading.value = false;
  }
}

function roleLabel(role: string) {
  if (role === "Owner") {
    return t("elections.admins.roleOwner");
  }
  return t("elections.admins.roleAdmin");
}

function personName(admin: ElectionAdmin) {
  return admin.displayName || admin.email;
}

async function addAdmin() {
  const trimmed = email.value.trim();
  if (!trimmed) {
    return;
  }

  adding.value = true;
  try {
    const result = await electionAdminService.add(props.electionGuid, trimmed);
    email.value = "";
    admins.value = await electionAdminService.list(props.electionGuid);
    showSuccessMessage(
      result.alreadyAdmin
        ? t("elections.admins.alreadyAdmin")
        : t("elections.admins.added"),
    );
  } catch (error: unknown) {
    handleApiError(error);
  } finally {
    adding.value = false;
  }
}

async function removeAdmin(admin: ElectionAdmin) {
  try {
    await ElMessageBox.confirm(
      t("elections.admins.removeConfirm", { name: personName(admin) }),
      t("elections.admins.removeTitle"),
      {
        confirmButtonText: t("elections.admins.remove"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
        confirmButtonClass: "el-button--danger",
      },
    );
  } catch {
    return;
  }

  removingId.value = admin.userId;
  try {
    await electionAdminService.remove(props.electionGuid, admin.userId);
    admins.value = await electionAdminService.list(props.electionGuid);
    showSuccessMessage(t("elections.admins.removed"));
  } catch (error: unknown) {
    handleApiError(error);
  } finally {
    removingId.value = null;
  }
}
</script>

<template>
  <section v-if="visible" class="election-admins">
    <h3>{{ $t("elections.admins.title") }}</h3>
    <p class="admins-help">{{ $t("elections.admins.help") }}</p>

    <div class="admins-add">
      <el-input
        v-model="email"
        type="email"
        data-testid="election-admin-email"
        :placeholder="$t('elections.admins.emailPlaceholder')"
        @keyup.enter="addAdmin"
      />
      <el-button
        type="primary"
        data-testid="add-election-admin"
        :loading="adding"
        :disabled="loading"
        @click="addAdmin"
      >
        {{ $t("elections.admins.add") }}
      </el-button>
    </div>

    <ul
      v-loading="loading"
      class="admin-list"
      data-testid="election-admin-list"
    >
      <li
        v-for="admin in admins"
        :key="admin.userId"
        class="admin-row"
        :data-testid="`election-admin-${admin.userId}`"
      >
        <div class="admin-identity">
          <span class="admin-name">{{ personName(admin) }}</span>
          <span v-if="admin.displayName" class="admin-email">{{
            admin.email
          }}</span>
        </div>
        <span class="admin-role">{{ roleLabel(admin.role) }}</span>
        <el-button
          v-if="admin.canRemove"
          type="danger"
          link
          :data-testid="`remove-election-admin-${admin.userId}`"
          :loading="removingId === admin.userId"
          @click="removeAdmin(admin)"
        >
          {{ $t("elections.admins.remove") }}
        </el-button>
      </li>
    </ul>
  </section>
</template>

<style lang="less">
.election-admins {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-lighter);

  h3 {
    margin: 0 0 8px;
    font-size: 16px;
  }

  .admins-help {
    margin: 0 0 12px;
    color: #909399;
    font-size: 13px;
  }

  .admins-add {
    display: flex;
    gap: 8px;
    max-width: 480px;
    margin-bottom: 12px;
  }

  .admin-list {
    margin: 0;
    padding: 0;
    list-style: none;
  }

  .admin-row {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 0;
    border-bottom: 1px solid var(--el-border-color-lighter);
  }

  .admin-identity {
    flex: 1;
    min-width: 0;
  }

  .admin-name {
    display: block;
  }

  .admin-email {
    display: block;
    color: #909399;
    font-size: 12px;
  }

  .admin-role {
    color: #606266;
  }
}
</style>
