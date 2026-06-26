<script setup lang="ts">
import {
  getAssistantTellerMenuPages,
  getAssistantTellerRedirectPath,
  isAssistantTellerRouteAllowed,
} from "@/domain/assistantTellerAccess";
import {
  type ElectionStage,
  type NavPageDef,
  STAGES,
  STAGE_META,
  STAGE_PAGES,
} from "@/domain/electionStages";
import { useNavUiStore } from "@/stores/navUiStore";
import { ElIcon } from "element-plus";
import { onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

const props = defineProps<{
  electionGuid: string;
  currentStage: ElectionStage;
  isTeller: boolean;
}>();

const emit = defineEmits<{
  "close-mobile-sidebar": [];
}>();

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const navUiStore = useNavUiStore();

function assistantTellerPages(): NavPageDef[] {
  return getAssistantTellerMenuPages(props.currentStage, props.electionGuid);
}

function isExpanded(stage: ElectionStage): boolean {
  const stored = navUiStore.sidebarGroupExpansion[stage];
  return stored !== undefined ? stored : stage === props.currentStage;
}

function handleToggleGroup(stage: ElectionStage) {
  const current = isExpanded(stage);
  navUiStore.setGroupExpanded(stage, !current);
}

function isActivePage(page: NavPageDef): boolean {
  return route.path === page.routePath(props.electionGuid);
}

function navigateTo(page: NavPageDef) {
  router.push(page.routePath(props.electionGuid));
  emit("close-mobile-sidebar");
}

onMounted(() => {
  for (const stage of STAGES) {
    if (navUiStore.sidebarGroupExpansion[stage] === undefined) {
      navUiStore.setGroupExpanded(stage, stage === props.currentStage);
    }
  }
});

watch(
  () => props.currentStage,
  (newStage) => {
    if (navUiStore.sidebarGroupExpansion[newStage] === undefined) {
      navUiStore.setGroupExpanded(newStage, true);
    }
  },
);

watch(
  [() => props.currentStage, () => route.path],
  () => {
    if (!props.isTeller) {
      return;
    }

    if (
      !isAssistantTellerRouteAllowed(
        route.path,
        props.electionGuid,
        props.currentStage,
      )
    ) {
      router.push(
        getAssistantTellerRedirectPath(props.electionGuid, props.currentStage),
      );
    }
  },
  { immediate: true },
);
</script>

<template>
  <div
    class="stage-grouped-menu"
    role="navigation"
    aria-label="Election navigation"
  >
    <!-- Teller view: no group headers, just the filtered page list for current stage -->
    <template v-if="isTeller">
      <div class="stage-group__pages">
        <div
          v-for="page in assistantTellerPages()"
          :key="page.key"
          class="stage-group__page"
          :class="{ 'is-active': isActivePage(page) }"
          role="menuitem"
          @click="navigateTo(page)"
        >
          <el-icon class="stage-group__page-icon" aria-hidden="true">
            <component :is="page.icon" />
          </el-icon>
          <span>{{ t(page.i18nKey) }}</span>
        </div>
      </div>
    </template>

    <!-- Admin view: all stage groups, collapsible -->
    <template v-else>
      <template v-for="stage in STAGES" :key="stage">
        <div
          class="stage-group"
          :class="{
            'is-active-stage': stage === currentStage,
            'is-expanded': isExpanded(stage),
          }"
        >
          <div
            class="stage-group__header"
            role="button"
            :aria-expanded="isExpanded(stage)"
            :aria-current="stage === currentStage ? 'true' : undefined"
            :style="{
              borderLeftColor: `var(${STAGE_META[stage].colorVar})`,
              backgroundColor:
                stage === currentStage
                  ? `var(${STAGE_META[stage].bgVar})`
                  : undefined,
            }"
            @click="handleToggleGroup(stage)"
          >
            <span class="stage-group__header-chip">
              <el-icon class="stage-group__header-icon">
                <component :is="STAGE_META[stage].icon" />
              </el-icon>
              <span>{{ t(STAGE_META[stage].groupI18nKey) }}</span>
            </span>
            <span
              class="stage-group__header-chevron"
              :class="{ 'is-expanded': isExpanded(stage) }"
            >
              ›
            </span>
          </div>

          <div v-if="isExpanded(stage)" class="stage-group__pages">
            <div
              v-for="page in STAGE_PAGES[stage]"
              :key="page.key"
              class="stage-group__page"
              :class="{ 'is-active': isActivePage(page) }"
              role="menuitem"
              @click="navigateTo(page)"
            >
              <el-icon class="stage-group__page-icon" aria-hidden="true">
                <component :is="page.icon" />
              </el-icon>
              <span>{{ t(page.i18nKey) }}</span>
            </div>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<style lang="less">
.stage-grouped-menu {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;

  .stage-group {
    margin-bottom: 4px;

    &__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px 8px 16px;
      cursor: pointer;
      border-left: 3px solid transparent;
      font-size: 13px;
      font-weight: 600;
      color: var(--color-sidebar-text);
      transition: background-color 0.15s;
      user-select: none;

      &:hover {
        background-color: var(--color-sidebar-hover);
      }

      &--teller {
        cursor: default;
        justify-content: flex-start;
        gap: 8px;

        &:hover {
          background-color: inherit;
        }
      }

      &-chip {
        display: flex;
        align-items: center;
        gap: 6px;
      }

      &-icon {
        font-size: 15px;
        flex-shrink: 0;
      }

      &-chevron {
        font-size: 16px;
        transition: transform 0.2s;
        transform: rotate(90deg);

        &.is-expanded {
          transform: rotate(-90deg);
        }
      }
    }

    &__pages {
      overflow: hidden;
    }

    &__page {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 12px 10px 32px;
      cursor: pointer;
      font-size: 14px;
      color: var(--color-sidebar-text);
      border-radius: 6px;
      margin: 2px 10px;
      height: 44px;
      box-sizing: border-box;
      transition: background-color 0.15s;

      &:hover {
        background-color: var(--color-sidebar-hover) !important;
      }

      &.is-active {
        background-color: var(--color-sidebar-active) !important;
        color: var(--color-sidebar-text-active) !important;
      }

      &-icon {
        font-size: 16px;
        flex-shrink: 0;
      }
    }
  }
}
</style>
