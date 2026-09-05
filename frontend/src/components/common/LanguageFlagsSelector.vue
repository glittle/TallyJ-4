<script setup lang="ts">
import { setLocale, supportedLocales, type SupportedLocale } from "@/locales";
import { applyDocumentLocale } from "@/locales/localeDirection";
import { computed, onMounted } from "vue";
import CountryFlag from "vue-country-flag-next";
import { useI18n } from "vue-i18n";

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
  applyDocumentLocale(locale.value);
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
  flex-wrap: wrap;
  align-items: center;
  justify-content: center;
  gap: 0.15rem;
  padding: 0.5rem;
  /* :lang(ar|fa) sets text-align:right on every element; keep the group centered */
  text-align: center;
  background-color: #fff4e5;
  color: #8c4a00;
  border: 2px solid #f5a23d;
  border-radius: 10px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);

  .flag-button {
    background: transparent;
    border: 1px solid transparent;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;
    /* Sprite crop is authored LTR; isolate so RTL dir does not shift flags */
    direction: ltr;
    unicode-bidi: isolate;
    text-align: center;
    width: 2.25rem;
    height: 1.75rem;
    overflow: hidden;
    padding: 0;

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
      margin-block: -14px;
      margin-inline: -26px;
      font-size: 24px;
    }
  }
}
</style>
