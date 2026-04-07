<script setup lang="ts">
import { ref } from "vue";
import AppHeader from "../components/AppHeader.vue";
import AppSidebar from "../components/AppSidebar.vue";

const mobileSidebarOpen = ref(false);

function toggleMobileSidebar() {
  mobileSidebarOpen.value = !mobileSidebarOpen.value;
}

function closeMobileSidebar() {
  mobileSidebarOpen.value = false;
}
</script>

<template>
  <el-container class="main-layout">
    <!-- Skip link for keyboard navigation -->
    <a href="#main-content" class="skip-link">Skip to main content</a>

    <!-- Mobile sidebar overlay -->
    <div
      v-if="mobileSidebarOpen"
      class="mobile-sidebar-overlay"
      @click="closeMobileSidebar"
    ></div>

    <el-aside
      width="300px"
      class="sidebar"
      role="complementary"
      aria-label="Main navigation"
      :class="{ 'mobile-sidebar-open': mobileSidebarOpen }"
    >
      <AppSidebar @close-mobile-sidebar="closeMobileSidebar" />
    </el-aside>
    <el-container>
      <el-header height="60px" role="banner">
        <AppHeader @toggle-mobile-menu="toggleMobileSidebar" />
      </el-header>
      <el-main id="main-content" role="main" tabindex="-1">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<style lang="less">
.main-layout {
  height: 100vh;
  background: var(--color-public-bg-gradient);

  .skip-link {
    position: absolute;
    top: -40px;
    left: 6px;
    padding: 8px;
    text-decoration: none;
    border-radius: 4px;
    z-index: 1000;
    font-weight: 500;
  }

  .skip-link:focus {
    top: 6px;
  }

  .sidebar {
    background-color: var(--color-sidebar-bg);
    box-shadow: 2px 0 6px rgba(0, 0, 0, 0.1);
    overflow-x: hidden;
  }

  /* Mobile sidebar overlay */
  .mobile-sidebar-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 1000;
  }

  .sidebar.mobile-sidebar-open {
    transform: translateX(0);
    z-index: 1001;
  }

  .el-header {
    background: var(--color-public-header-bg);
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
    display: flex;
    align-items: center;
    padding: 0 20px;
  }

  .el-main {
    background-color: var(--color-public-bg-gradient);
    padding: 20px;
    overflow-y: auto;
    max-width: none;
  }
}

@media (max-width: 768px) {
  .main-layout {
    .sidebar {
      width: 180px !important;
      position: fixed;
      top: 60px;
      left: 0;
      height: calc(100vh - 60px);
      transform: translateX(-100%);
      transition: transform 0.3s ease;
      z-index: 1001;
    }

    .sidebar.mobile-sidebar-open {
      transform: translateX(0);
    }

    .el-main {
      padding: 10px;
    }
  }
}

@media (max-width: 480px) {
  .main-layout {
    .sidebar {
      width: 250px !important;
    }
  }
}

/* On very wide screens, constrain main content for readability */
@media (min-width: 1400px) {
  .main-layout {
    .el-main {
      // max-width: 1400px;
      margin: 0 auto;
      width: 100%;
    }
  }
}
</style>
