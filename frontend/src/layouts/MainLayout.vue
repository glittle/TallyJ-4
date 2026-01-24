<template>
  <el-container class="main-layout">
    <el-aside width="200px" class="sidebar">
      <AppSidebar />
    </el-aside>
    <el-container>
      <el-header height="60px">
        <AppHeader />
      </el-header>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import AppHeader from '../components/AppHeader.vue';
import AppSidebar from '../components/AppSidebar.vue';
</script>

<style scoped>
.main-layout {
  height: 100vh;
}

.sidebar {
  background-color: #304156;
  box-shadow: 2px 0 6px rgba(0, 0, 0, 0.1);
  overflow-x: hidden;
}

@media (max-width: 768px) {
  .sidebar {
    width: 180px !important;
  }
}

@media (max-width: 480px) {
  .sidebar {
    display: none;
  }
}

:deep(.el-header) {
  background-color: #fff;
  border-bottom: 1px solid #dcdfe6;
  display: flex;
  align-items: center;
  padding: 0 20px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

:deep(.el-main) {
  background-color: #f0f2f5;
  padding: 20px;
  overflow-y: auto;
}

@media (max-width: 768px) {
  :deep(.el-main) {
    padding: 10px;
  }
}
</style>
