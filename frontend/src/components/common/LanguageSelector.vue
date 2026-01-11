<template>
  <el-select
    v-model="currentLocale"
    @change="changeLocale"
    style="width: 120px"
    size="small"
  >
    <el-option
      v-for="lang in availableLanguages"
      :key="lang.code"
      :label="lang.name"
      :value="lang.code"
    >
      {{ lang.name }}
    </el-option>
  </el-select>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { setLocale } from '../../locales';

type Locale = 'en' | 'fr';

const { locale } = useI18n();
const currentLocale = ref(locale.value);

const availableLanguages = [
  { code: 'en' as Locale, name: 'English' },
  { code: 'fr' as Locale, name: 'Français' }
];

const changeLocale = (newLocale: string) => {
  if (newLocale === 'en' || newLocale === 'fr') {
    setLocale(newLocale);
    currentLocale.value = newLocale;
  }
};

watch(locale, (newLocale) => {
  currentLocale.value = newLocale;
});
</script>
