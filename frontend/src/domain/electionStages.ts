import type { Component } from "vue";
import {
  DataAnalysis,
  Document,
  Files,
  Location,
  Monitor,
  PieChart,
  Setting,
  Tickets,
  User,
  UserFilled,
} from "@element-plus/icons-vue";

export type ElectionStage =
  | "SettingUp"
  | "GatheringBallots"
  | "ProcessingBallots";

export interface NavPageDef {
  key: string;
  i18nKey: string;
  icon: Component;
  routePath: (electionGuid: string) => string;
  adminOnly?: boolean;
}

export interface StageMeta {
  key: ElectionStage;
  i18nKey: string;
  shortI18nKey: string;
  groupI18nKey: string;
  colorVar: string;
  bgVar: string;
  icon: Component;
}

export const STAGES: readonly ElectionStage[] = [
  "SettingUp",
  "GatheringBallots",
  "ProcessingBallots",
] as const;

export const STAGE_META: Record<ElectionStage, StageMeta> = {
  SettingUp: {
    key: "SettingUp",
    i18nKey: "elections.stage.SettingUp",
    shortI18nKey: "elections.stage.SettingUp_short",
    groupI18nKey: "elections.stageNav.group.SettingUp",
    colorVar: "--color-stage-setup",
    bgVar: "--color-stage-setup-bg",
    icon: Setting,
  },
  GatheringBallots: {
    key: "GatheringBallots",
    i18nKey: "elections.stage.GatheringBallots",
    shortI18nKey: "elections.stage.GatheringBallots_short",
    groupI18nKey: "elections.stageNav.group.GatheringBallots",
    colorVar: "--color-stage-gather",
    bgVar: "--color-stage-gather-bg",
    icon: Monitor,
  },
  ProcessingBallots: {
    key: "ProcessingBallots",
    i18nKey: "elections.stage.ProcessingBallots",
    shortI18nKey: "elections.stage.ProcessingBallots_short",
    groupI18nKey: "elections.stageNav.group.ProcessingBallots",
    colorVar: "--color-stage-process",
    bgVar: "--color-stage-process-bg",
    icon: PieChart,
  },
};

export const STAGE_PAGES: Record<ElectionStage, NavPageDef[]> = {
  SettingUp: [
    {
      key: "details",
      i18nKey: "elections.details",
      icon: Document,
      routePath: (g) => `/elections/${g}`,
      adminOnly: true,
    },
    {
      key: "edit",
      i18nKey: "elections.edit",
      icon: Setting,
      routePath: (g) => `/elections/${g}/edit`,
      adminOnly: true,
    },
    {
      key: "people",
      i18nKey: "people.management",
      icon: User,
      routePath: (g) => `/elections/${g}/people`,
      adminOnly: true,
    },
    {
      key: "locations",
      i18nKey: "nav.votingLocations",
      icon: Location,
      routePath: (g) => `/elections/${g}/locations`,
      adminOnly: true,
    },
    {
      key: "tellers",
      i18nKey: "nav.tellers",
      icon: UserFilled,
      routePath: (g) => `/elections/${g}/tellers`,
      adminOnly: true,
    },
  ],
  GatheringBallots: [
    {
      key: "frontdesk",
      i18nKey: "nav.frontDesk",
      icon: Monitor,
      routePath: (g) => `/elections/${g}/frontdesk`,
    },
  ],
  ProcessingBallots: [
    {
      key: "ballots",
      i18nKey: "ballots.management",
      icon: Tickets,
      routePath: (g) => `/elections/${g}/ballots`,
    },
    {
      key: "tally",
      i18nKey: "results.calculateTally",
      icon: PieChart,
      routePath: (g) => `/elections/${g}/tally`,
      adminOnly: true,
    },
    {
      key: "monitor",
      i18nKey: "results.monitor",
      icon: Monitor,
      routePath: (g) => `/elections/${g}/monitor`,
      adminOnly: true,
    },
    {
      key: "results",
      i18nKey: "results.title",
      icon: DataAnalysis,
      routePath: (g) => `/elections/${g}/results`,
      adminOnly: true,
    },
    {
      key: "reporting",
      i18nKey: "results.reporting",
      icon: Files,
      routePath: (g) => `/elections/${g}/reporting`,
      adminOnly: true,
    },
  ],
};

export function tallyStatusToStage(s?: string | null): ElectionStage {
  switch (s) {
    case "Setup":
    case "Draft":
      return "SettingUp";
    case "Voting":
    case "Counting":
    case "Gathering":
      return "GatheringBallots";
    case "Tallying":
    case "Finalized":
    case "Processing":
    case "Archived":
      return "ProcessingBallots";
    default:
      return "SettingUp";
  }
}

export function stageToTallyStatus(stage: ElectionStage): string {
  switch (stage) {
    case "SettingUp":
      return "Setup";
    case "GatheringBallots":
      return "Counting";
    case "ProcessingBallots":
      return "Finalized";
  }
}
