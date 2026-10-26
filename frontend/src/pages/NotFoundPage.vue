<script setup lang="ts">
import { secureTokenService } from "@/services/secureTokenService";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

const { t } = useI18n();
const router = useRouter();

const homePath = computed(() =>
  secureTokenService.isAuthenticated() ? "/dashboard" : "/",
);

const homeLabel = computed(() =>
  secureTokenService.isAuthenticated() ? t("nav.dashboard") : t("error.goHome"),
);

function goHome() {
  router.push(homePath.value);
}
</script>

<template>
  <div class="not-found-page">
    <h1>{{ $t("error.pageNotFound.title") }}</h1>
    <p>{{ $t("error.pageNotFound.message") }}</p>
    <el-button type="primary" @click="goHome">
      {{ homeLabel }}
    </el-button>
  </div>
</template>

<style lang="less">
.not-found-page {
  max-width: 36rem;
  margin: 4rem auto;
  padding: 2rem;
  text-align: center;

  h1 {
    margin-bottom: 0.75rem;
  }

  p {
    margin-bottom: 1.5rem;
  }
}
</style>
