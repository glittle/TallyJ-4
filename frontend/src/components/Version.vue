<script setup lang="ts">
/**
 * Version component
 * Stores the current version number and build date
 * Update this file before deploying to production
 */
export interface VersionInfo {
  version: string;
  date: string;
}

const versionInfo: VersionInfo = {
  version: '4.0.1 Beta',
  date: new Date().toISOString().split('T')[0] // YYYY-MM-DD format
};

// Export for use in other components
defineExpose({
  versionInfo
});
</script>

<template>
  <!-- This component is not meant to be rendered -->
  <div style="display: none"></div>
</template>
