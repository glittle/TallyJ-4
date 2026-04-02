<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { onMounted, computed } from "vue";
import CountryFlag from "vue-country-flag-next";
import { setLocale, supportedLocales, type SupportedLocale } from "@/locales";

const { locale, t } = useI18n();

const languages = computed(() =>
  supportedLocales.map((lang) => ({
    ...lang,
    label: t(lang.name),
  })),
);

const changeLanguage = async (lang: string) => {
  await setLocale(lang as SupportedLocale);
};

const isActive = (lang: string) => {
  return locale.value === lang;
};

onMounted(() => {
  document.documentElement.lang = locale.value;
});
</script>

<template>
  <div class="language-flags-selector">
    <button
      v-for="lang in languages"
      :key="lang.value"
      :class="['flag-button', { active: isActive(lang.value) }]"
      :aria-label="`${lang.label}`"
      :title="lang.label"
      @click="changeLanguage(lang.value)"
    >
      <country-flag :country="lang.flag" size="normal" />
    </button>
  </div>
</template>

<style lang="less">
.language-flags-selector {
  display: flex;
  align-items: center;
  flex-wrap: wrap;

  .flag-button {
    background: transparent;
    border: 1px solid transparent;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;

    &:hover {
      background: var(--el-fill-color-light);
      border-color: var(--el-border-color);
    }

    &.active {
      border-color: var(--el-color-primary);
      background: var(--el-fill-color);
    }

    &:focus-visible {
      outline: 2px solid var(--el-color-primary);
      outline-offset: 2px;
    }

    span.flag {
      display: block;
      margin: -14px -26px;
      font-size: 24px;
    }
  }
}
</style>
