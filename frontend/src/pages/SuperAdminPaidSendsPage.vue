<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import {
  superAdminService,
  type PaidSendOverview,
} from "../services/superAdminService";

const { t } = useI18n();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const loading = ref(false);
const overview = ref<PaidSendOverview>({
  pendingOwners: [],
  capHits: [],
  frozenElections: [],
  frozenOwners: [],
  flaggedElections: [],
});
const raiseDrafts = ref<Record<string, number>>({});
const freezeElectionId = ref("");
const freezeOwnerId = ref("");

async function load() {
  loading.value = true;
  try {
    overview.value = await superAdminService.getPaidSends();
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
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
  } catch (err) {
    showErrorMessage(extractApiErrorMessage(err));
    loading.value = false;
  }
}

function draftKey(kind: string, id: string) {
  return kind + ":" + id;
}

function draftValue(kind: string, id: string, current: number) {
  return raiseDrafts.value[draftKey(kind, id)] ?? current + 1;
}

function setDraft(kind: string, id: string, value: number | undefined) {
  if (value == null) {
    return;
  }
  raiseDrafts.value[draftKey(kind, id)] = value;
}

function reasonLabel(reason?: string | null) {
  if (!reason) {
    return "";
  }
  const key = "superAdmin.paidSends.reason." + reason;
  const translated = t(key);
  return translated === key ? reason : translated;
}

onMounted(load);
</script>

<template>
  <div class="super-admin-paid-sends-page">
    <div class="page-header">
      <h1>{{ $t("superAdmin.paidSends.title") }}</h1>
      <router-link to="/super-admin" class="back-link">
        {{ $t("superAdmin.title") }}
      </router-link>
    </div>

    <el-card v-loading="loading">
      <h2>{{ $t("superAdmin.paidSends.pendingTitle") }}</h2>
      <p v-if="overview.pendingOwners.length === 0" class="empty">
        {{ $t("superAdmin.paidSends.pendingEmpty") }}
      </p>
      <el-table v-else :data="overview.pendingOwners" stripe>
        <el-table-column
          prop="email"
          :label="$t('superAdmin.paidSends.email')"
          min-width="200"
        />
        <el-table-column
          prop="displayName"
          :label="$t('superAdmin.paidSends.name')"
          min-width="140"
        />
        <el-table-column
          prop="electionCount"
          :label="$t('superAdmin.paidSends.elections')"
          width="110"
        />
        <el-table-column :label="$t('superAdmin.paidSends.approve')" width="280">
          <template #default="{ row }">
            <el-button
              type="primary"
              size="small"
              @click="
                run(
                  () => superAdminService.approvePaidSends(row.userId),
                  'superAdmin.paidSends.approved',
                )
              "
            >
              {{ $t("superAdmin.paidSends.approve") }}
            </el-button>
            <el-button
              size="small"
              @click="
                run(
                  () => superAdminService.freezeOwner(row.userId),
                  'superAdmin.paidSends.frozen',
                )
              "
            >
              {{ $t("superAdmin.paidSends.freeze") }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card>
      <h2>{{ $t("superAdmin.paidSends.capHitsTitle") }}</h2>
      <p v-if="overview.capHits.length === 0" class="empty">
        {{ $t("superAdmin.paidSends.capHitsEmpty") }}
      </p>
      <el-table v-else :data="overview.capHits" stripe>
        <el-table-column :label="$t('superAdmin.paidSends.scope')" min-width="160">
          <template #default="{ row }">
            {{
              row.scope === "election"
                ? $t("superAdmin.paidSends.scopeElection")
                : $t("superAdmin.paidSends.scopeOwner")
            }}
          </template>
        </el-table-column>
        <el-table-column :label="$t('superAdmin.paidSends.name')" min-width="180">
          <template #default="{ row }">
            {{ row.electionName || row.ownerEmail || row.ownerUserId }}
          </template>
        </el-table-column>
        <el-table-column prop="used" :label="$t('superAdmin.paidSends.used')" width="80" />
        <el-table-column prop="cap" :label="$t('superAdmin.paidSends.cap')" width="80" />
        <el-table-column :label="$t('superAdmin.paidSends.raise')" width="240">
          <template #default="{ row }">
            <el-input-number
              :model-value="
                draftValue(
                  row.scope,
                  row.electionGuid || row.ownerUserId,
                  row.cap,
                )
              "
              :min="row.cap + 1"
              @update:model-value="
                setDraft(row.scope, row.electionGuid || row.ownerUserId, $event)
              "
            />
            <el-button
              size="small"
              type="primary"
              @click="
                run(async () => {
                  const next = draftValue(
                    row.scope,
                    row.electionGuid || row.ownerUserId,
                    row.cap,
                  );
                  if (row.scope === 'election') {
                    await superAdminService.raiseElectionAllowance(
                      row.electionGuid,
                      next,
                    );
                  } else {
                    await superAdminService.raiseOwnerDailyCap(
                      row.ownerUserId,
                      next,
                    );
                  }
                }, 'superAdmin.paidSends.raised')
              "
            >
              {{ $t("superAdmin.paidSends.raise") }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card>
      <h2>{{ $t("superAdmin.paidSends.frozenElectionsTitle") }}</h2>
      <p v-if="overview.frozenElections.length === 0" class="empty">
        {{ $t("superAdmin.paidSends.frozenEmpty") }}
      </p>
      <el-table v-else :data="overview.frozenElections" stripe>
        <el-table-column prop="name" :label="$t('superAdmin.paidSends.name')" />
        <el-table-column width="140">
          <template #default="{ row }">
            <el-button
              size="small"
              @click="
                run(
                  () => superAdminService.unfreezeElection(row.electionGuid),
                  'superAdmin.paidSends.unfrozen',
                )
              "
            >
              {{ $t("superAdmin.paidSends.unfreeze") }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card>
      <h2>{{ $t("superAdmin.paidSends.frozenOwnersTitle") }}</h2>
      <p v-if="overview.frozenOwners.length === 0" class="empty">
        {{ $t("superAdmin.paidSends.frozenEmpty") }}
      </p>
      <el-table v-else :data="overview.frozenOwners" stripe>
        <el-table-column prop="email" :label="$t('superAdmin.paidSends.email')" />
        <el-table-column prop="displayName" :label="$t('superAdmin.paidSends.name')" />
        <el-table-column width="140">
          <template #default="{ row }">
            <el-button
              size="small"
              @click="
                run(
                  () => superAdminService.unfreezeOwner(row.userId),
                  'superAdmin.paidSends.unfrozen',
                )
              "
            >
              {{ $t("superAdmin.paidSends.unfreeze") }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-card>
      <h2>{{ $t("superAdmin.paidSends.freezeByIdTitle") }}</h2>
      <div class="freeze-row">
        <el-input
          v-model="freezeElectionId"
          :placeholder="$t('superAdmin.paidSends.electionId')"
        />
        <el-button
          :disabled="!freezeElectionId"
          @click="
            run(
              () => superAdminService.freezeElection(freezeElectionId.trim()),
              'superAdmin.paidSends.frozen',
            )
          "
        >
          {{ $t("superAdmin.paidSends.freezeElection") }}
        </el-button>
      </div>
      <div class="freeze-row">
        <el-input
          v-model="freezeOwnerId"
          :placeholder="$t('superAdmin.paidSends.ownerId')"
        />
        <el-button
          :disabled="!freezeOwnerId"
          @click="
            run(
              () => superAdminService.freezeOwner(freezeOwnerId.trim()),
              'superAdmin.paidSends.frozen',
            )
          "
        >
          {{ $t("superAdmin.paidSends.freezeOwner") }}
        </el-button>
      </div>
    </el-card>

    <el-card>
      <h2>{{ $t("superAdmin.paidSends.flaggedTitle") }}</h2>
      <p v-if="overview.flaggedElections.length === 0" class="empty">
        {{ $t("superAdmin.paidSends.flaggedEmpty") }}
      </p>
      <div
        v-for="election in overview.flaggedElections"
        :key="election.electionGuid"
        class="flagged-election"
      >
        <div class="flagged-header">
          <strong>{{ election.name }}</strong>
          <el-button
            size="small"
            type="primary"
            @click="
              run(
                () => superAdminService.clearElectionFlag(election.electionGuid),
                'superAdmin.paidSends.flagCleared',
              )
            "
          >
            {{ $t("superAdmin.paidSends.clearFlag") }}
          </el-button>
        </div>
        <el-table :data="election.rows" stripe>
          <el-table-column
            prop="rowNumber"
            :label="$t('superAdmin.paidSends.row')"
            width="80"
          />
          <el-table-column
            prop="maskedValue"
            :label="$t('superAdmin.paidSends.value')"
            min-width="160"
          />
          <el-table-column :label="$t('superAdmin.paidSends.reason')" min-width="160">
            <template #default="{ row }">
              {{ reasonLabel(row.reason) }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-card>
  </div>
</template>

<style lang="less">
.super-admin-paid-sends-page {
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

  .el-card {
    margin-bottom: 16px;
  }

  h2 {
    margin: 0 0 12px;
    font-size: 1.1rem;
  }

  .empty {
    margin: 0;
    color: var(--el-text-color-secondary);
  }

  .flagged-election {
    margin-bottom: 16px;
  }

  .flagged-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
  }

  .freeze-row {
    display: flex;
    gap: 12px;
    margin-bottom: 12px;
    max-width: 640px;
  }
}
</style>
