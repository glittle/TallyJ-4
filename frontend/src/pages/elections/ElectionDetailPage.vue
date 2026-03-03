<template>
  <div class="election-detail-page">
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>

    <div v-else-if="election">
      <el-page-header @back="goBack" :content="election.name">
        <template #extra>
          <el-button type="primary" @click="editElection">
            <el-icon>
              <Edit />
            </el-icon>
            {{ $t('common.edit') }}
          </el-button>
        </template>
      </el-page-header>

      <el-row :gutter="20" style="margin-top: 20px;">
        <el-col :span="18">
          <el-card class="info-card">
            <template #header>
              <span>{{ $t('elections.details') }}</span>
            </template>
            <el-descriptions :column="2" border>
              <el-descriptions-item :label="$t('elections.form.name')">
                {{ election.name }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.type')">
                {{ election.electionType || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.date')">
                {{ formatDate(election.dateOfElection) }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.status')">
                <el-tag :type="getStatusType(election.tallyStatus)">
                  {{ election.tallyStatus || 'Draft' }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.numberToElect')">
                {{ election.numberToElect }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.numberExtra')">
                {{ election.numberExtra }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.convenor')">
                {{ election.convenor || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('elections.form.electionMode')">
                {{ election.electionMode || '-' }}
              </el-descriptions-item>
            </el-descriptions>
          </el-card>

          <el-card class="actions-card" style="margin-top: 20px;">
            <template #header>
              <span>{{ $t('elections.quickActions') }}</span>
            </template>
            <el-space wrap :size="15">
              <el-button @click="managePeople">
                <el-icon>
                  <UserFilled />
                </el-icon>
                {{ $t('elections.managePeople') }}
              </el-button>
              <el-button @click="manageLocations">
                <el-icon>
                  <LocationFilled />
                </el-icon>
                {{ $t('elections.manageLocations') }}
              </el-button>
              <el-button @click="manageBallots">
                <el-icon>
                  <Tickets />
                </el-icon>
                {{ $t('elections.manageBallots') }}
              </el-button>
              <el-button @click="openFrontDesk">
                <el-icon>
                  <Check />
                </el-icon>
                {{ $t('nav.frontDesk') }}
              </el-button>
              <el-button @click="viewResults">
                <el-icon>
                  <DataAnalysis />
                </el-icon>
                {{ $t('elections.viewResults') }}
              </el-button>
              <el-button @click="viewReports">
                <el-icon>
                  <Document />
                </el-icon>
                {{ $t('elections.viewReports') }}
              </el-button>
              <el-button @click="calculateTally" type="warning">
                <el-icon>
                  <Operation />
                </el-icon>
                {{ $t('elections.calculateTally') }}
              </el-button>
            </el-space>
          </el-card>

          <el-card class="teller-access-card" style="margin-top: 20px;">
            <template #header>
              <span>{{ $t('elections.tellerAccess') }}</span>
            </template>

            <div class="access-status">
              <el-tag :type="election?.isTellerAccessOpen ? 'success' : 'info'">
                {{ election?.isTellerAccessOpen ? $t('elections.tellerAccessOpen') : $t('elections.tellerAccessClosed')
                }}
              </el-tag>
            </div>

            <div class="access-status" style="margin-top: 10px;">
              <el-tag :type="onlineVotingStatus ? 'success' : 'info'">
                {{ onlineVotingStatus ? $t('elections.onlineVotingEnabled') : $t('elections.onlineVotingDisabled') }}
              </el-tag>
            </div>

            <el-divider />

            <el-button type="primary" @click="toggleTellerAccess" :loading="loading" style="margin-bottom: 15px;">
              {{ $t('elections.toggleTellerAccess') }}
            </el-button>

            <div v-if="election?.electionPasscode" class="access-details">
              <div class="access-item">
                <label>{{ $t('elections.tellerAccessCode') }}:</label>
                <el-input :model-value="election.electionPasscode" readonly style="margin-top: 5px;" />
              </div>

              <div class="access-item" style="margin-top: 15px;">
                <label>{{ $t('elections.tellerAccessUrl') }}:</label>
                <div class="url-container">
                  <el-input :model-value="shareableUrl" readonly style="margin-top: 5px;" />
                  <el-button type="primary" @click="copyUrl" :icon="CopyDocument" style="margin-left: 10px;">
                    {{ $t('elections.copyUrl') }}
                  </el-button>
                </div>
              </div>

              <div class="access-item" style="margin-top: 15px;">
                <label>{{ $t('elections.tellerAccessQrCode') }}:</label>
                <div class="qr-container" style="margin-top: 10px;">
                  <img v-if="qrCodeUrl" :src="qrCodeUrl" alt="QR Code" class="qr-code" />
                  <div v-else class="qr-placeholder">
                    <el-icon size="48">
                      <Link />
                    </el-icon>
                    <p>{{ $t('common.loading') }}</p>
                  </div>
                </div>
              </div>
            </div>

            <div v-else class="no-passcode">
              <el-alert type="warning" :title="$t('common.warning')"
                :description="$t('elections.form.electionPasscodeHelp')" show-icon />
            </div>
          </el-card>
        </el-col>

        <el-col :span="6">
          <el-card class="stats-card">
            <template #header>
              <span>{{ $t('elections.statistics') }}</span>
            </template>
            <div class="stat-item">
              <div class="stat-label">{{ $t('dashboard.totalVoters') }}</div>
              <div class="stat-value">{{ election.voterCount }}</div>
            </div>
            <el-divider />
            <div class="stat-item">
              <div class="stat-label">{{ $t('dashboard.totalBallots') }}</div>
              <div class="stat-value">{{ election.ballotCount }}</div>
            </div>
            <el-divider />
            <div class="stat-item">
              <div class="stat-label">{{ $t('elections.locations') }}</div>
              <div class="stat-value">{{ election.locationCount }}</div>
            </div>
          </el-card>
        </el-col>
      </el-row>
      <el-row>
        <el-card class="danger-zone" style="margin-top: 20px;">
          <template #header>
            <span style="color: #f56c6c;">{{ $t('common.dangerZone') }}</span>
          </template>
          <el-button type="danger" plain @click="confirmDelete" style="width: 100%;">
            <el-icon>
              <Delete />
            </el-icon>
            {{ $t('common.delete') }}
          </el-button>
        </el-card>
      </el-row>
    </div>

    <el-empty v-else :description="$t('elections.notFound')" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ElMessageBox } from 'element-plus';
import { Edit, UserFilled, LocationFilled, Tickets, DataAnalysis, Operation, Delete, Check, CopyDocument, Link, Document } from '@element-plus/icons-vue';
import { useElectionStore } from '../../stores/electionStore';
import { useNotifications } from '@/composables/useNotifications';
import QRCode from 'qrcode';

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const loading = computed(() => electionStore.loading);
const election = computed(() => electionStore.currentElection);

const qrCodeUrl = ref('');
const shareableUrl = computed(() => {
  if (!election.value?.electionPasscode) return '';
  const baseUrl = window.location.origin;
  return `${baseUrl}/teller-join/${electionGuid}?code=${encodeURIComponent(election.value.electionPasscode)}`;
});

const onlineVotingStatus = computed(() => {
  // Check if online voting is enabled based on onlineWhenOpen and onlineWhenClose
  return election.value?.onlineWhenOpen && election.value?.onlineWhenClose;
});

onMounted(async () => {
  try {
    await electionStore.initializeSignalR();
    await electionStore.fetchElectionById(electionGuid);
    await electionStore.joinElection(electionGuid);
    await generateQrCode();
  } catch (error) {
    showErrorMessage(t('elections.loadError'));
  }
});

// Watch for changes to shareable URL to regenerate QR code
watch(shareableUrl, async (newUrl) => {
  if (newUrl) {
    await generateQrCode();
  }
});

onUnmounted(async () => {
  try {
    await electionStore.leaveElection(electionGuid);
  } catch (error) {
    console.error('Failed to leave election:', error);
  }
});

function goBack() {
  router.push('/elections');
}

function editElection() {
  router.push(`/elections/${electionGuid}/edit`);
}

function managePeople() {
  router.push(`/elections/${electionGuid}/people`);
}

function manageLocations() {
  router.push(`/elections/${electionGuid}/locations`);
}

function manageBallots() {
  router.push(`/elections/${electionGuid}/ballots`);
}

function openFrontDesk() {
  router.push(`/elections/${electionGuid}/frontdesk`);
}

function viewResults() {
  router.push(`/elections/${electionGuid}/results`);
}

function viewReports() {
  router.push(`/elections/${electionGuid}/reporting`);
}

function calculateTally() {
  router.push(`/elections/${electionGuid}/tally`);
}

async function confirmDelete() {
  try {
    await ElMessageBox.confirm(
      t('elections.deleteConfirm'),
      t('common.warning'),
      {
        confirmButtonText: t('common.delete'),
        cancelButtonText: t('common.cancel'),
        type: 'warning',
        confirmButtonClass: 'el-button--danger'
      }
    );

    await electionStore.deleteElection(electionGuid);
    showSuccessMessage(t('elections.deleteSuccess'));
    router.push('/elections');
  } catch (error: any) {
    if (error !== 'cancel') {
      showErrorMessage(error.message || t('elections.deleteError'));
    }
  }
}

function formatDate(date?: string) {
  if (!date) return '-';
  return new Date(date).toLocaleDateString();
}

function getStatusType(status?: string) {
  const typeMap: Record<string, any> = {
    'Draft': 'info',
    'Voting': 'success',
    'Tallying': 'warning',
    'Finalized': 'info'
  };
  return typeMap[status || ''] || 'info';
}

async function toggleTellerAccess() {
  if (!election.value) return;

  try {
    const newState = !election.value.isTellerAccessOpen;
    await electionStore.toggleTellerAccess(electionGuid, newState);
    showSuccessMessage(
      newState
        ? t('elections.tellerAccessOpen')
        : t('elections.tellerAccessClosed')
    );
  } catch (error) {
    showErrorMessage(t('common.error'));
  }
}

async function copyUrl() {
  if (!shareableUrl.value) return;

  try {
    await navigator.clipboard.writeText(shareableUrl.value);
    showSuccessMessage(t('common.copied'));
  } catch (error) {
    showErrorMessage(t('common.error'));
  }
}

async function generateQrCode() {
  if (!shareableUrl.value) return;

  try {
    qrCodeUrl.value = await QRCode.toDataURL(shareableUrl.value, {
      width: 200,
      margin: 2,
      color: {
        dark: '#000000',
        light: '#FFFFFF'
      }
    });
  } catch (error) {
    console.error('Failed to generate QR code:', error);
  }
}
</script>

<style lang="less">
.election-detail-page {
  max-width: 1400px;
  margin: 0 auto;
}

.loading-container {
  padding: 40px;
}

.info-card,
.actions-card,
.stats-card,
.danger-zone,
.teller-access-card {
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.stat-item {
  text-align: center;
  padding: 10px 0;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #303133;
}

.el-divider {
  margin: 12px 0;
}

.access-status {
  text-align: center;
}

.access-details {
  .access-item {
    label {
      font-weight: 600;
      color: #303133;
      display: block;
    }

    .url-container {
      display: flex;
      align-items: flex-start;
      gap: 10px;

      .el-input {
        flex: 1;
      }
    }

    .qr-container {
      text-align: center;

      .qr-code {
        max-width: 200px;
        height: auto;
        border: 1px solid #e4e7ed;
        border-radius: 4px;
      }

      .qr-placeholder {
        padding: 40px;
        border: 1px dashed #d9d9d9;
        border-radius: 4px;
        color: #909399;

        p {
          margin: 10px 0 0 0;
          font-size: 14px;
        }
      }
    }
  }
}

.no-passcode {
  margin-top: 15px;
}
</style>
