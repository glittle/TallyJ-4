<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import LanguageSelector from "../components/common/LanguageSelector.vue";
import LanguageFlagsSelector from "../components/common/LanguageFlagsSelector.vue";
import ThemeSelector from "../components/common/ThemeSelector.vue";
import { VERSION, BUILD_DATE } from "../components/version";

const route = useRoute();

// Version tooltip - static string computed once
const versionTooltip = `Version ${VERSION} (${BUILD_DATE})`;

// Check if we're on the landing page
const isLandingPage = computed(() => route.path === "/" || route.name === "landing");
</script>

<template>
  <div class="public-layout">
    <div class="public-header">
      <div class="logo">
        <h2 :title="versionTooltip">
          <img
            src="/logo-zoom.png"
            alt="TallyJ Logo"
            style="height: 24px; vertical-align: middle; margin-left: 8px"
          />
          <span>{{ $t('title') }}</span>
        </h2>
      </div>
      <div class="header-right">
        <ThemeSelector />
        <LanguageFlagsSelector v-if="isLandingPage" />
        <LanguageSelector v-else />
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
  .public-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 40px;
    backdrop-filter: blur(10px);
    /* Fallback for browsers that don't support backdrop-filter */
    background: var(--color-public-header-bg);
    -webkit-backdrop-filter: blur(10px); /* Safari support */
  }

  .logo h2 {
    display: flex;
    align-items: center;

    gap: 10px;
    color: var(--color-public-text);
    margin: 0;
    font-size: 24px;
    font-weight: 600;
    cursor: help;

    img {
      background-color: rgba(255, 255, 255, 0.4);
      padding: 4px;
      border-radius: 4px;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 20px;
  }

  .public-content {
    padding: 20px;
  }
}
</style>
