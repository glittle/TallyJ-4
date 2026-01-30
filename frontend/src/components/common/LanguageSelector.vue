<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElSelect, ElOption } from "element-plus";
import { onMounted } from "vue";

const { locale, t } = useI18n();

const languages = [
  { value: "en", label: t("common.english") },
  { value: "fr", label: t("common.french") },
];

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
    <ElSelect
      id="language-select"
      :model-value="locale"
      @update:model-value="changeLanguage"
      size="small"
      aria-label="Select language"
    >
      <ElOption
        v-for="lang in languages"
        :key="lang.value"
        :label="lang.label"
        :value="lang.value"
      />
    </ElSelect>
  </div>
</template>

<style lang="less">
.language-selector {
  display: inline-block;

  .el-select {
    width: 120px;

    .el-input__inner {
      color: #333;
    }

    .el-select-dropdown {
      .el-select-dropdown__item {
        color: #333;
      }
    }
  }
}
</style>
