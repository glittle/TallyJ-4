<script setup lang="ts">
import { computed } from "vue";
import { useRouter } from "vue-router";
import LanguageFlagsSelector from "../components/common/LanguageFlagsSelector.vue";
import LanguageSelector from "../components/common/LanguageSelector.vue";
import ThemeSelector from "../components/common/ThemeSelector.vue";
import { BUILD_DATE, VERSION } from "../components/version";

const router = useRouter();
// const { t } = useI18n();

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
          <img src="/assets/logo-trans.png" :alt="$t('common.logoAlt')" />
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
    gap: 2em;
    align-items: center;
    padding: 20px 40px;
    backdrop-filter: blur(10px);
    /* Fallback for browsers that don't support backdrop-filter */
    background: var(--color-public-header-bg);
    -webkit-backdrop-filter: blur(10px);
    /* Safari support */
  }

  .header-middle {
    flex-grow: 1;
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
      height: 2em;
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
}
</style>
