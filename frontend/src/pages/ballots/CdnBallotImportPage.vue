<script setup lang="ts">
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { UploadFilled } from "@element-plus/icons-vue";
import type { UploadFile, UploadRawFile } from "element-plus";
import { useNotifications } from "@/composables/useNotifications";
import { electionService } from "../../services/electionService";
import type { ImportResultDto } from "../../types";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const { showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const importing = ref(false);
const importResult = ref<ImportResultDto | null>(null);

function goBack() {
  router.push(`/elections/${electionGuid}/ballots`);
}

async function handleFileChange(uploadFile: UploadFile) {
  const file = uploadFile.raw as UploadRawFile | undefined;
  if (!file) return;

  importing.value = true;
  importResult.value = null;

  try {
    const result = await electionService.importCdnBallots(electionGuid, file);
    importResult.value = result;
  } catch (error: any) {
    showErrorMessage(error.message || t("ballots.cdnImport.failed"));
  } finally {
    importing.value = false;
  }
}

function beforeUpload(file: UploadRawFile) {
  const isXml =
    file.type === "text/xml" ||
    file.type === "application/xml" ||
    file.name.toLowerCase().endsWith(".xml");
  if (!isXml) {
    showErrorMessage(t("ballots.cdnImport.xmlOnly"));
    return false;
  }
  return true;
}
</script>

<template>
  <div class="cdn-ballot-import-page">
    <el-card>
      <template #header>
        <div class="card-header">
          <el-page-header
            :content="$t('ballots.cdnImport.title')"
            @back="goBack"
          />
        </div>
      </template>

      <el-alert
        type="info"
        :closable="false"
        show-icon
        style="margin-bottom: 24px"
      >
        <template #default>
          {{ $t("ballots.cdnImport.description") }}
        </template>
      </el-alert>

      <div v-if="importing" class="importing-state">
        <el-skeleton :rows="3" animated />
        <p class="importing-label">{{ $t("ballots.cdnImport.importing") }}</p>
      </div>

      <template v-else>
        <el-upload
          class="cdn-upload"
          drag
          :auto-upload="false"
          accept=".xml"
          :show-file-list="false"
          :before-upload="beforeUpload"
          :on-change="handleFileChange"
        >
          <el-icon class="el-icon--upload">
            <UploadFilled />
          </el-icon>
          <div class="el-upload__text">
            {{ $t("ballots.cdnImport.uploadPrompt") }}
          </div>
          <template #tip>
            <div class="el-upload__tip">
              {{ $t("ballots.cdnImport.xmlOnly") }}
            </div>
          </template>
        </el-upload>

        <div v-if="importResult" class="import-result">
          <el-result
            v-if="importResult.success"
            icon="success"
            :title="$t('ballots.cdnImport.success')"
          >
            <template #extra>
              <el-descriptions :column="2" border class="result-descriptions">
                <el-descriptions-item :label="$t('ballots.cdnImport.ballotsCreated')">
                  {{ importResult.ballotsCreated }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('ballots.cdnImport.votesCreated')">
                  {{ importResult.votesCreated }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('ballots.cdnImport.totalRows')">
                  {{ importResult.totalRows }}
                </el-descriptions-item>
                <el-descriptions-item :label="$t('ballots.cdnImport.skippedRows')">
                  {{ importResult.skippedRows }}
                </el-descriptions-item>
              </el-descriptions>

              <div
                v-if="importResult.warnings && importResult.warnings.length > 0"
                class="warnings-section"
              >
                <h4>{{ $t("ballots.cdnImport.warnings") }}</h4>
                <el-alert
                  v-for="(warning, index) in importResult.warnings"
                  :key="index"
                  type="warning"
                  :title="warning"
                  :closable="false"
                  show-icon
                  style="margin-bottom: 8px"
                />
              </div>

              <el-button type="primary" style="margin-top: 16px" @click="goBack">
                {{ $t("ballots.management") }}
              </el-button>
            </template>
          </el-result>

          <el-result
            v-else
            icon="error"
            :title="$t('ballots.cdnImport.failed')"
          >
            <template #extra>
              <div
                v-if="importResult.errors && importResult.errors.length > 0"
                class="errors-section"
              >
                <h4>{{ $t("ballots.cdnImport.errors") }}</h4>
                <el-alert
                  v-for="(error, index) in importResult.errors"
                  :key="index"
                  type="error"
                  :title="error"
                  :closable="false"
                  show-icon
                  style="margin-bottom: 8px"
                />
              </div>

              <el-button style="margin-top: 16px" @click="importResult = null">
                {{ $t("error.tryAgain") }}
              </el-button>
            </template>
          </el-result>
        </div>
      </template>
    </el-card>
  </div>
</template>

<style lang="less">
.cdn-ballot-import-page {
  max-width: 800px;
  margin: 0 auto;

  .card-header {
    display: flex;
    align-items: center;
  }

  .cdn-upload {
    width: 100%;
    margin-bottom: 24px;

    .el-upload-dragger {
      width: 100%;
    }
  }

  .importing-state {
    text-align: center;
    padding: 40px;

    .importing-label {
      margin-top: 16px;
      color: #606266;
      font-size: 14px;
    }
  }

  .import-result {
    margin-top: 24px;

    .result-descriptions {
      margin-bottom: 16px;
      text-align: left;
    }

    .warnings-section,
    .errors-section {
      text-align: left;
      margin-top: 16px;

      h4 {
        margin: 0 0 8px 0;
        font-size: 14px;
        font-weight: 600;
        color: #303133;
      }
    }
  }
}
</style>
