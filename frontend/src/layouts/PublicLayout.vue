<script setup lang="ts">
import { ElConfigProvider } from "element-plus";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import LanguageFlagsSelector from "../components/common/LanguageFlagsSelector.vue";
import LanguageSelector from "../components/common/LanguageSelector.vue";
import ThemeSelector from "../components/common/ThemeSelector.vue";
import { getBuildDate, getBuildDateBadi, VERSION } from "../components/version";
import { localeTextDirection } from "../locales/localeDirection";
import { splitVersionDisplay } from "../locales/versionDisplay";

const router = useRouter();
const { locale, t } = useI18n();

// Version tooltip - dynamically localized
const versionName = computed(() => VERSION);
const versionDate = computed(() => getBuildDate());
const versionDateBadi = computed(() => getBuildDateBadi());
const versionDisplayParts = computed(() =>
  splitVersionDisplay(t("common.versionDisplay")),
);
const textDirection = computed(() => localeTextDirection(locale.value));

// Check if we're on the landing page
const expandLanguageSelector = true; // computed(() => route.path === "/" || route.name === "landing");

const logoSrc = "/assets/logo-trans.png";

const handleLogoClick = () => {
  router.push("/");
};
</script>

<template>
  <ElConfigProvider :direction="textDirection">
    <div class="public-layout">
      <div class="public-header">
        <div class="logo" style="cursor: pointer" @click="handleLogoClick">
          <h2>
            <img :src="logoSrc" :alt="$t('common.logoAlt')" />
            <span>
              <div class="version-display">
                <span>{{ versionDisplayParts.before }}</span>
                <span v-if="versionDisplayParts.mark" class="version-beta">{{
                  versionDisplayParts.mark
                }}</span>
                <span>{{ versionDisplayParts.after }}</span>
              </div>
              <div
                class="versionName"
                :title="versionDate + ' - ' + versionDateBadi"
              >
                {{ versionName }}
              </div>
            </span>
          </h2>
        </div>
        <div class="header-middle">
          <LanguageFlagsSelector v-if="expandLanguageSelector" />
          <LanguageSelector v-else />
        </div>
        <div class="header-right">
          <ThemeSelector />
        </div>
      </div>
      <div class="public-content">
        <router-view />
      </div>
    </div>
  </ElConfigProvider>
</template>

<style lang="less">
.public-layout {
  min-height: 100vh;
  background: var(--color-public-bg-gradient);

  .versionName {
    font-size: 0.5em;
    color: var(--color-text-secondary);
  }

  .version-beta {
    font-weight: var(--font-weight-semibold);
    font-family: var(--font-family-primary);
  }

  .public-header {
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    grid-template-areas: "logo flags theme";
    column-gap: 2em;
    row-gap: 1em;
    align-items: center;
    padding: 20px 40px;
    backdrop-filter: blur(10px);
    /* Fallback for browsers that don't support backdrop-filter */
    background: var(--color-public-header-bg);
    -webkit-backdrop-filter: blur(10px);
    /* Safari support */

    .logo {
      grid-area: logo;
      justify-self: start;
    }

    .header-middle {
      grid-area: flags;
      justify-self: center;
    }

    .header-right {
      grid-area: theme;
      justify-self: end;
    }

    @media (max-width: 600px) {
      grid-template-columns: 1fr auto;
      grid-template-areas:
        "logo theme"
        "flags flags";
      padding: 12px 16px;
    }
  }

  .header-middle {
    display: flex;
    justify-content: center;
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
    text-align: end;
    align-items: center;
    gap: 20px;
  }

  .public-content {
    padding: 20px;
  }
}
</style>
