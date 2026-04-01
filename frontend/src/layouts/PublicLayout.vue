<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import LanguageFlagsSelector from "../components/common/LanguageFlagsSelector.vue";
import LanguageSelector from "../components/common/LanguageSelector.vue";
import ThemeSelector from "../components/common/ThemeSelector.vue";
import { BUILD_DATE, VERSION } from "../components/version";

const router = useRouter();
const { t } = useI18n();

// Version tooltip - dynamically localized
const versionName = computed(() => VERSION);
const versionDate = computed(() => BUILD_DATE);

// Check if we're on the landing page
const expandLanguageSelector = true; // computed(() => route.path === "/" || route.name === "landing");

const handleLogoClick = () => {
  router.push("/");
};
</script>

<template>
  <div class="public-layout">
    <div class="public-header">
      <div class="logo" style="cursor: pointer" @click="handleLogoClick">
        <h2>
          <img
            src="/logo-zoom.png"
            :alt="$t('common.logoAlt')"
            style="height: 24px; vertical-align: middle; margin-left: 8px"
          />
          <span>
            <div>{{ $t("common.versionDisplay") }}</div>
            <div class="versionName" :title="versionDate">
              {{ versionName }}
            </div>
          </span>
        </h2>
      </div>
      <div class="header-middle">
        <div>
          <LanguageFlagsSelector v-if="expandLanguageSelector" />
          <LanguageSelector v-else />
        </div>
      </div>
      <div class="header-right">
        <ThemeSelector />
      </div>
    </div>
    <div class="public-content">
      <router-view />
    </div>
  </div>
</template>

<style lang="less">
.public-layout {
  min-height: 100vh;
  background: var(--color-public-bg-gradient);

  .versionName {
    font-size: 0.5em;
    color: var(--color-text-secondary);
  }

  .public-header {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    gap: 1em;
    align-items: center;
    padding: 20px 40px;
    backdrop-filter: blur(10px);
    /* Fallback for browsers that don't support backdrop-filter */
    background: var(--color-public-header-bg);
    -webkit-backdrop-filter: blur(10px);
    /* Safari support */
  }

  .header-middle {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
  }

  .logo h2 {
    display: flex;
    align-items: center;

    gap: 10px;
    color: var(--color-public-text);
    margin: 0;
    font-size: 24px;
    font-weight: 600;

    img {
      background-color: rgba(255, 255, 255, 0.4);
      padding: 4px;
      border-radius: 4px;
    }
  }

  .header-right {
    text-align: right;
    align-items: center;
    gap: 20px;
  }

  .public-content {
    padding: 20px;
  }
</style>
