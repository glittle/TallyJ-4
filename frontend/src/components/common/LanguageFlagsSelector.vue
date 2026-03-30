<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { onMounted } from "vue";
import CountryFlag from "vue-country-flag-next";
import { setLocale, type SupportedLocale } from "@/locales";

const { locale } = useI18n();

const languages = [
  { value: "en", flag: "us", label: "English" },
  { value: "fr", flag: "fr", label: "Français" },
  { value: "fi", flag: "fi", label: "Suomi" },
  { value: "ko", flag: "kr", label: "한국어" },
  { value: "es", flag: "es", label: "Español" },
  { value: "pt", flag: "br", label: "Português" },
  { value: "hi", flag: "in", label: "हिन्दी" },
  { value: "vi", flag: "vn", label: "Tiếng Việt" },
  { value: "fa", flag: "ir", label: "فارسی" },
  { value: "sw", flag: "tz", label: "Kiswahili" },
  { value: "ar", flag: "sa", label: "العربية" },
  { value: "zh", flag: "cn", label: "中文" },
  { value: "ru", flag: "ru", label: "Русский" },
];

const changeLanguage = async (lang: string) => {
  await setLocale(lang as SupportedLocale);
  locale.value = lang;
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
