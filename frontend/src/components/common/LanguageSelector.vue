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
    <label for="language-select" class="sr-only">{{ $t('common.language') }}</label>
    <ElSelect
      id="language-select"
      :model-value="locale"
      @update:model-value="changeLanguage"
      size="small"
      style="width: 120px"
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
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.language-selector {
  display: inline-block;
}
</style>
