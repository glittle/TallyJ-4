<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { ElSelect, ElOption } from 'element-plus'

const { locale, t } = useI18n()

const languages = [
  { value: 'en', label: t('common.english') },
  { value: 'fr', label: t('common.french') }
]

const changeLanguage = (lang: string) => {
  locale.value = lang
  localStorage.setItem('preferred-language', lang)
}
</script>

<template>
  <div class="language-selector">
    <ElSelect
      :model-value="locale"
      @update:model-value="changeLanguage"
      size="small"
      style="width: 120px"
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
}
</style>
