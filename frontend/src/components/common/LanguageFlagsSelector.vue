<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { onMounted } from "vue";
import CountryFlag from "vue-country-flag-next";

const { locale } = useI18n();

const languages = [
  { value: "en", flag: "us", label: "English" },
  { value: "fr", flag: "fr", label: "Français" },
];

const changeLanguage = (lang: string) => {
  locale.value = lang;
  localStorage.setItem("preferred-language", lang);
  document.documentElement.lang = lang;
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
      :aria-label="`Switch to ${lang.label}`"
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
  gap: 8px;
  align-items: center;

  .flag-button {
    background: transparent;
    border: 2px solid transparent;
    border-radius: 4px;
    cursor: pointer;
    padding: 4px 8px;
    transition: all 0.2s ease;
    display: flex;
    align-items: center;
    justify-content: center;

    &:hover {
      background: rgba(255, 255, 255, 0.1);
      border-color: rgba(255, 255, 255, 0.3);
    }

    &.active {
      border-color: rgba(255, 255, 255, 0.6);
      background: rgba(255, 255, 255, 0.15);
    }

    &:focus-visible {
      outline: 2px solid var(--el-color-primary);
      outline-offset: 2px;
    }

    span.flag {
      display: block;
      font-size: 24px;
    }
  }
}
</style>
