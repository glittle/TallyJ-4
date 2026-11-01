<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import {
  superAdminService,
  type OwnerReviewDetail,
  type OwnerReviewCount,
} from "../services/superAdminService";

const { t } = useI18n();
const route = useRoute();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const loading = ref(false);
const detail = ref<OwnerReviewDetail | null>(null);

const userId = computed(() => String(route.params.userId || ""));

function formatWhen(value?: string | Date | null) {
  if (!value) {
    return t("superAdmin.ownerReview.none");
  }
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) {
    return t("superAdmin.ownerReview.none");
  }
  return date.toLocaleString();
}

function formatCounts(counts?: OwnerReviewCount[] | null) {
  if (!counts || counts.length === 0) {
    return t("superAdmin.ownerReview.none");
  }
  return counts.map((item) => `${item.key} ${item.count ?? 0}`).join(", ");
}

function statusLabel(value?: string | null) {
  if (value === "ReviewRequested") {
    return t("superAdmin.ownerReview.statusReviewRequested");
  }
  if (value === "Rejected") {
    return t("superAdmin.ownerReview.statusRejected");
  }
  if (value === "Approved") {
    return t("superAdmin.ownerReview.statusApproved");
  }
  return t("superAdmin.ownerReview.statusRestricted");
}

async function load() {
  if (!userId.value) {
    return;
  }
  loading.value = true;
  try {
    detail.value = await superAdminService.getOwnerReview(userId.value);
  } catch (error) {
    showErrorMessage(extractApiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

async function run(action: () => Promise<void>, successKey: string) {
  loading.value = true;
  try {
    await action();
    showSuccessMessage(t(successKey));
    await load();
  } catch (error) {
    showErrorMessage(extractApiErrorMessage(error));
    loading.value = false;
  }
}

async function approve() {
  await run(
    () => superAdminService.approveOwnerReview(userId.value),
    "superAdmin.ownerReview.approved",
  );
}

async function reject() {
  try {
    const { value } = await ElMessageBox.prompt(
      t("superAdmin.ownerReview.rejectConfirm"),
      t("superAdmin.ownerReview.reject"),
      {
        inputType: "textarea",
        inputPlaceholder: t("superAdmin.ownerReview.notePrompt"),
      },
    );
    await run(
      () => superAdminService.rejectOwnerReview(userId.value, value),
      "superAdmin.ownerReview.rejectedDone",
    );
  } catch {
    // Cancelled.
  }
}

async function hold() {
  try {
    const { value } = await ElMessageBox.prompt(
      t("superAdmin.ownerReview.notePrompt"),
      t("superAdmin.ownerReview.hold"),
      { inputType: "textarea" },
    );
    await run(
      () => superAdminService.holdOwnerReview(userId.value, value),
      "superAdmin.ownerReview.held",
    );
  } catch {
    // Cancelled.
  }
}

async function deleteNow() {
  try {
    await ElMessageBox.confirm(
      t("superAdmin.ownerReview.deleteConfirm"),
      t("superAdmin.ownerReview.deleteNow"),
      { type: "warning" },
    );
    await run(
      () => superAdminService.deleteRejectedOwnerElections(userId.value),
      "superAdmin.ownerReview.deleted",
    );
  } catch {
    // Cancelled.
  }
}

onMounted(load);
watch(userId, load);
</script>

<template>
  <div v-loading="loading" class="super-admin-owner-review-detail">
    <div class="page-header">
      <h1>{{ detail?.email || t("superAdmin.ownerReview.title") }}</h1>
      <router-link to="/super-admin/owner-reviews" class="back-link">
        {{ t("superAdmin.ownerReview.back") }}
      </router-link>
    </div>

    <template v-if="detail">
      <el-descriptions :column="1" border class="owner-facts">
        <el-descriptions-item :label="t('superAdmin.ownerReview.name')">
          {{ detail.displayName }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('superAdmin.ownerReview.status')">
          {{ statusLabel(detail.status) }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('superAdmin.ownerReview.signIn')">
          {{ detail.authMethod }}
        </el-descriptions-item>
        <el-descriptions-item :label="t('superAdmin.ownerReview.signedUp')">
          {{ formatWhen(detail.signedUpAt) }}
        </el-descriptions-item>
        <el-descriptions-item
          v-if="detail.deleteElectionsAt"
          :label="t('superAdmin.ownerReview.deleteAt')"
        >
          {{ formatWhen(detail.deleteElectionsAt) }}
        </el-descriptions-item>
        <el-descriptions-item
          v-if="detail.note"
          :label="t('superAdmin.ownerReview.notePrompt')"
        >
          {{ detail.note }}
        </el-descriptions-item>
      </el-descriptions>

      <div class="review-actions">
        <el-button type="primary" @click="approve">
          {{ t("superAdmin.ownerReview.approve") }}
        </el-button>
        <el-button @click="hold">{{
          t("superAdmin.ownerReview.hold")
        }}</el-button>
        <el-button type="danger" @click="reject">
          {{ t("superAdmin.ownerReview.reject") }}
        </el-button>
        <el-button
          v-if="detail.status === 'Rejected'"
          type="danger"
          plain
          @click="deleteNow"
        >
          {{ t("superAdmin.ownerReview.deleteNow") }}
        </el-button>
      </div>

      <p
        v-if="!detail.elections || detail.elections.length === 0"
        class="empty"
      >
        {{ t("superAdmin.ownerReview.noElections") }}
      </p>

      <el-card
        v-for="election in detail.elections || []"
        :key="election.electionGuid"
        class="election-card"
      >
        <template #header>
          <strong>{{ election.name }}</strong>
        </template>
        <p>
          {{ t("superAdmin.ownerReview.type") }}:
          {{ election.electionType || t("superAdmin.ownerReview.none") }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.dates") }}:
          {{ formatWhen(election.dateOfElection) }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.online") }}:
          {{ formatWhen(election.onlineWhenOpen) }}
          –
          {{ formatWhen(election.onlineWhenClose) }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.people") }}:
          {{ election.peopleCount ?? 0 }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.domains") }}:
          {{ formatCounts(election.emailDomains) }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.phones") }}:
          {{ formatCounts(election.phoneCountries) }}
        </p>
        <p>
          {{ t("superAdmin.ownerReview.flagged") }}:
          {{ election.flaggedContactCount ?? 0 }}
        </p>
        <p v-if="election.flaggedSample && election.flaggedSample.length">
          {{ t("superAdmin.ownerReview.sample") }}:
          {{ election.flaggedSample.join("; ") }}
        </p>
        <p v-if="election.peopleSample && election.peopleSample.length">
          {{ t("superAdmin.ownerReview.peopleSample") }}:
          {{ election.peopleSample.join("; ") }}
        </p>
      </el-card>
    </template>
  </div>
</template>

<style lang="less">
.super-admin-owner-review-detail {
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;

    h1 {
      margin: 0;
    }
  }

  .owner-facts {
    margin-bottom: 16px;
  }

  .review-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 20px;
  }

  .election-card {
    margin-bottom: 16px;

    p {
      margin: 0 0 6px;
    }
  }
}
</style>
