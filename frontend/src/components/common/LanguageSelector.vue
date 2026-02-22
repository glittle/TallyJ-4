<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElSelect, ElOption } from "element-plus";
import { onMounted } from "vue";
import CountryFlag from "vue-country-flag-next";

const { locale, t } = useI18n();

const languages = [
  { value: "en", flag: "us", label: t("english") },
  { value: "fr", flag: "fr", label: t("french") },
];

const getFlag = (lang: string) => {
  const langObj = languages.find((l) => l.value === lang);
  return langObj ? langObj.flag : "us";
};

const changeLanguage = (lang: string) => {
  locale.value = lang;
  localStorage.setItem("preferred-language", lang);
  document.documentElement.lang = lang;
};

onMounted(() => {
  document.documentElement.lang = locale.value;
});
</script>

<template>
  <div class="language-selector">
    <label for="language-select" class="sr-only">{{ $t("common.language") }}</label>
    <el-select
      id="language-select"
      :model-value="locale"
      @update:model-value="changeLanguage"
      size="small"
      filterable
      aria-label="Select language"
      popper-class="language-select-dropdown"
    >
      <template #label="{ label, value }">
        <country-flag :country="getFlag(value)" size="small" />
        {{ label }}
      </template>
      <el-option
        v-for="lang in languages"
        :key="lang.value"
        :label="lang.label"
        :value="lang.value"
      >
        <template #default>
          <country-flag :country="lang.flag" size="small" />
          {{ lang.label }}
        </template>
      </el-option>
    </el-select>
  </div>
</template>

<style lang="less">
.language-selector {
  display: inline-block;

  // Example of how to style based on language
  // :lang(fr){
  //   background-color: red;
  // }

  .el-select {
    width: 120px;
    span.flag {
      margin-right: -0.75em;
    }

    .el-input__inner {
      color: var(--el-text-color-primary);
      background-color: var(--el-fill-color-blank);
    }
    .el-select__placeholder {
      display: inline-flex;
      align-items: center;
    }
  }
}
.language-select-dropdown {
  span.flag {
    margin-right: -0.75em;
  }
}
</style>
