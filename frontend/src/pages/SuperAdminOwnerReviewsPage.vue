<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import {
  superAdminService,
  type OwnerReviewListItem,
} from "../services/superAdminService";

const { t } = useI18n();
const router = useRouter();
const { showErrorMessage } = useNotifications();

const loading = ref(false);
const status = ref("");
const rows = ref<OwnerReviewListItem[]>([]);

const filters = [
  { value: "", label: "superAdmin.ownerReview.filterAll" },
  { value: "ReviewRequested", label: "superAdmin.ownerReview.filterRequested" },
  { value: "Restricted", label: "superAdmin.ownerReview.filterRestricted" },
  { value: "Rejected", label: "superAdmin.ownerReview.filterRejected" },
];

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
  loading.value = true;
  try {
    rows.value = await superAdminService.listOwnerReviews(
      status.value || undefined,
    );
  } catch (error) {
    showErrorMessage(extractApiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

function openRow(row: OwnerReviewListItem) {
  if (!row.userId) {
    return;
  }
  router.push(`/super-admin/owner-reviews/${row.userId}`);
}

onMounted(load);
</script>

<template>
  <div class="super-admin-owner-reviews-page">
    <div class="page-header">
      <h1>{{ t("superAdmin.ownerReview.listTitle") }}</h1>
      <router-link to="/super-admin" class="back-link">
        {{ t("superAdmin.title") }}
      </router-link>
    </div>

    <div class="filters">
      <el-radio-group v-model="status" @change="load">
        <el-radio-button
          v-for="filter in filters"
          :key="filter.value"
          :value="filter.value"
        >
          {{ t(filter.label) }}
        </el-radio-button>
      </el-radio-group>
    </div>

    <el-table
      v-loading="loading"
      :data="rows"
      stripe
      class="review-table"
      @row-click="openRow"
    >
      <el-table-column
        prop="email"
        :label="t('superAdmin.ownerReview.email')"
        min-width="200"
      />
      <el-table-column
        prop="displayName"
        :label="t('superAdmin.ownerReview.name')"
        min-width="140"
      />
      <el-table-column
        :label="t('superAdmin.ownerReview.status')"
        min-width="160"
      >
        <template #default="{ row }">
          {{ statusLabel(row.status) }}
        </template>
      </el-table-column>
      <el-table-column
        prop="authMethod"
        :label="t('superAdmin.ownerReview.signIn')"
        width="120"
      />
      <el-table-column
        prop="electionCount"
        :label="t('superAdmin.ownerReview.elections')"
        width="110"
      />
      <el-table-column :label="t('superAdmin.ownerReview.open')" width="120">
        <template #default="{ row }">
          <el-button size="small" type="primary" @click.stop="openRow(row)">
            {{ t("superAdmin.ownerReview.open") }}
          </el-button>
        </template>
      </el-table-column>
      <template #empty>
        <span>{{ t("superAdmin.ownerReview.empty") }}</span>
      </template>
    </el-table>
  </div>
</template>

<style lang="less">
.super-admin-owner-reviews-page {
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;

    h1 {
      margin: 0;
    }
  }

  .filters {
    margin-bottom: 16px;
  }

  .review-table {
    cursor: pointer;
  }
}
</style>
