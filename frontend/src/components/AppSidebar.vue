<script setup lang="ts">
import {
  ArrowLeft,
  DataAnalysis,
  Document,
  Files,
  HomeFilled,
  Location,
  Monitor,
  PieChart,
  Setting,
  Tickets,
  User,
  UserFilled,
} from "@element-plus/icons-vue";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useElectionStore } from "../stores/electionStore";
import { useSuperAdminStore } from "../stores/superAdminStore";
import { BUILD_DATE, VERSION } from "./version";
const { t } = useI18n();

const emit = defineEmits<{
  "close-mobile-sidebar": [];
}>();

const route = useRoute();
const router = useRouter();
const electionStore = useElectionStore();
const superAdminStore = useSuperAdminStore();

const isSuperAdmin = computed(() => superAdminStore.isSuperAdmin);

const isInElectionContext = computed(() => {
  return route.path.startsWith("/elections/") && route.params.id;
});

const versionName = computed(() => VERSION);
const versionDate = computed(() => BUILD_DATE);

const electionName = computed(() => {
  return electionStore.currentElection?.name || t("common.election");
});

const activeRoute = computed(() => {
  const path = route.path;
  if (path.startsWith("/super-admin")) {
    return "/super-admin";
  }
  if (path.startsWith("/elections")) {
    // For election pages, return the full path to highlight the current page
    return path;
  }
  return "/dashboard";
});

function handleMenuSelect() {
  // Emit event to close mobile sidebar when menu item is selected
  emit("close-mobile-sidebar");
}

function goBackToElections() {
  router.push("/elections");
  emit("close-mobile-sidebar");
}
</script>
<template>
  <nav class="app-sidebar" role="navigation" aria-label="Main navigation">
    <div class="logo">
      <div class="logoTop">
        <img src="/assets/logo-trans.png" :alt="$t('common.logoAlt')" />
        <h2>{{ $t("common.appTitle") }}</h2>
      </div>
      <div class="version-tooltip" :title="versionDate">
        {{ $t("common.versionDisplay", { version: versionName }) }}
        -
        {{ versionName }}
      </div>
    </div>
    <div class="testOnlyWarning">
      {{ $t("common.testOnlyShort") }}
    </div>

    <!-- Election navigation menu -->
    <div v-if="isInElectionContext" class="election-nav">
      <div class="election-header">
        <div class="back-to-elections" @click="goBackToElections">
          <el-icon>
            <ArrowLeft />
          </el-icon>
          <span>{{ $t("nav.elections") }}</span>
        </div>
        <div class="election-title">
          <span>{{ electionName }}</span>
        </div>
      </div>

      <el-menu
        :default-active="activeRoute"
        :router="true"
        aria-label="Election navigation"
        class="election-menu"
        @select="handleMenuSelect"
      >
        <!-- Core Election Management -->
        <el-menu-item :index="`/elections/${route.params.id}`" role="menuitem">
          <el-icon aria-hidden="true">
            <Document />
          </el-icon>
          <span>{{ $t("elections.details") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/edit`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Setting />
          </el-icon>
          <span>{{ $t("elections.edit") }}</span>
        </el-menu-item>

        <!-- People & Voting -->
        <el-menu-item
          :index="`/elections/${route.params.id}/people`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <User />
          </el-icon>
          <span>{{ $t("people.management") }}</span>
        </el-menu-item>

        <!-- Election Setup -->
        <el-menu-item
          :index="`/elections/${route.params.id}/locations`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Location />
          </el-icon>
          <span>{{ $t("nav.votingLocations") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/tellers`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <UserFilled />
          </el-icon>
          <span>{{ $t("nav.tellers") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/frontdesk`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Monitor />
          </el-icon>
          <span>{{ $t("nav.frontDesk") }}</span>
        </el-menu-item>

        <!-- Ballots -->
        <el-menu-item
          :index="`/elections/${route.params.id}/ballots`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Tickets />
          </el-icon>
          <span>{{ $t("ballots.management") }}</span>
        </el-menu-item>

        <!-- Results & Reporting -->
        <el-menu-item
          :index="`/elections/${route.params.id}/results`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <DataAnalysis />
          </el-icon>
          <span>{{ $t("results.title") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/tally`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <PieChart />
          </el-icon>
          <span>{{ $t("results.calculateTally") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/monitor`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Monitor />
          </el-icon>
          <span>{{ $t("results.monitor") }}</span>
        </el-menu-item>

        <el-menu-item
          :index="`/elections/${route.params.id}/reporting`"
          role="menuitem"
        >
          <el-icon aria-hidden="true">
            <Files />
          </el-icon>
          <span>{{ $t("results.reporting") }}</span>
        </el-menu-item>
      </el-menu>
    </div>

    <!-- Main navigation menu -->
    <el-menu
      v-else
      :default-active="activeRoute"
      :router="true"
      aria-label="Main menu"
      @select="handleMenuSelect"
    >
      <el-menu-item index="/dashboard" role="menuitem">
        <el-icon aria-hidden="true">
          <HomeFilled />
        </el-icon>
        <span>{{ $t("nav.dashboard") }}</span>
      </el-menu-item>

      <el-menu-item index="/elections" role="menuitem">
        <el-icon aria-hidden="true">
          <Document />
        </el-icon>
        <span>{{ $t("nav.elections") }}</span>
      </el-menu-item>

      <el-menu-item v-if="isSuperAdmin" index="/super-admin" role="menuitem">
        <el-icon aria-hidden="true">
          <Setting />
        </el-icon>
        <span>{{ $t("nav.superAdmin") }}</span>
      </el-menu-item>
    </el-menu>

    <div class="statusDocLink">
      <a
        href="https://docs.google.com/document/d/1WXrVy2Jl3Lk-Vs1k77t2QnrrdK5zjzSeHMXnOx0Deao/edit?usp=sharing"
        target="statusDoc"
        >Status & Feedback Document V4</a
      >
    </div>
  </nav>
</template>

<style lang="less">
.app-sidebar {
  height: 100%;
  display: flex;
  flex-direction: column;

  .logo {
    padding: 20px;
    text-align: center;
    border-bottom: 1px solid var(--color-sidebar-border);
  }
  .logoTop {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 10px;
    img {
      height: 2.5em;
    }
  }

  .version-tooltip {
    text-align: center;
    font-size: 0.75em;
    color: var(--color-gray-500);
    margin: 4px 0 6px 0;
  }

  .logo h2 {
    color: var(--color-text-primary);
    margin: 0;
    font-size: 20px;
    font-weight: 600;
  }

  .el-menu {
    border-right: none;
    background-color: var(--color-sidebar-bg) !important;
    color: var(--color-sidebar-text) !important;
  }

  .el-menu-item {
    height: 50px;
    line-height: 50px;
  }

  .el-menu-item:hover {
    background-color: var(--color-sidebar-hover) !important;
  }

  .el-menu-item.is-active {
    background-color: var(--color-sidebar-active) !important;
    color: var(--color-sidebar-text-active) !important;
  }

  .election-nav {
    display: flex;
    flex-direction: column;
    height: calc(100% - 120px); // Account for logo and warning
  }

  .election-header {
    padding: 20px;
    border-bottom: 1px solid var(--color-sidebar-border);
  }

  .back-to-elections {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 0;
    cursor: pointer;
    transition: color 0.2s ease;
    font-size: 14px;
    color: var(--color-sidebar-text);

    &:hover {
      color: var(--color-sidebar-text-active);
    }

    .el-icon {
      font-size: 16px;
    }
  }

  .election-title {
    margin-top: 12px;
    font-weight: 600;
    font-size: 16px;
    color: var(--color-sidebar-text-active);
    word-break: break-word;
  }

  .election-menu {
    flex: 1;
    border-right: none;
    background-color: var(--color-sidebar-bg) !important;
    color: var(--color-sidebar-text) !important;
    padding: 10px 0;
    overflow-y: auto;

    .el-menu-item {
      height: 44px;
      line-height: 44px;
      margin: 2px 10px;
      border-radius: 6px;
      font-size: 14px;

      &:hover {
        background-color: var(--color-sidebar-hover) !important;
      }

      &.is-active {
        background-color: var(--color-sidebar-active) !important;
        color: var(--color-sidebar-text-active) !important;
      }

      .el-icon {
        margin-right: 8px;
        font-size: 16px;
      }
    }
  }

  .testOnlyWarning {
    padding: 1rem 0.5rem;
    margin: 0 10px;
    max-width: 920px; /* Comfortable reading width */
    text-align: center;
    background-color: #fff4e5;
    color: #8c4a00;
    font-size: 1.05rem;
    line-height: 1.5;
    border: 2px solid #f5a23d;
    border-radius: 10px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    font-weight: 500;
  }

  .statusDocLink {
    padding: 0 0.5em;
    text-align: center;
    margin: auto 0.5em 1em;
    background-color: #fff4e5;
    color: #8c4a00;
    border: 2px solid #f5a23d;
    border-radius: 4px;
    font-size: 1rem;
  }
}

:root.dark {
  .main-layout {
    .testOnlyWarning {
      background-color: #3f2a1a;
      color: #ffd9a8;
      border-color: #f5a23d;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);
    }
  }
}
</style>
