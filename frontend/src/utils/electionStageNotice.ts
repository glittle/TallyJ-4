import { ElMessage } from "element-plus";
import { h, type VNode } from "vue";
import { isFullTeller } from "@/domain/guestTellerAccess";
import {
  getStageWorkPagePath,
  STAGE_META,
  type ElectionStage,
} from "@/domain/electionStages";
import { i18n } from "@/locales";

/** Longer than the text-only toast so a FullTeller can click Go there. */
export const STAGE_NOTICE_GO_THERE_MS = 8000;
export const STAGE_NOTICE_TEXT_MS = 5000;

export function pathsEqualIgnoreCase(a: string, b: string): boolean {
  return a.toLowerCase() === b.toLowerCase();
}

function stageLabel(newStage: string): string {
  // Own-property check (not `in`) so prototype keys like "toString" never match.
  const meta = Object.hasOwn(STAGE_META, newStage)
    ? STAGE_META[newStage as ElectionStage]
    : undefined;
  const stageKey = meta ? meta.i18nKey : `elections.stage.${newStage}`;
  return i18n.global.t(stageKey);
}

function buildGoThereMessage(text: string, onGoThere: () => void): VNode {
  return h("span", { class: "election-stage-notice" }, [
    h("span", { class: "election-stage-notice__text" }, text),
    h(
      "button",
      {
        type: "button",
        class: "election-stage-notice__go-there",
        onClick: (event: MouseEvent) => {
          event.preventDefault();
          event.stopPropagation();
          onGoThere();
        },
      },
      i18n.global.t("elections.goToStagePage"),
    ),
  ]);
}

/**
 * Stage-change toast. GuestTellers auto-redirect separately and see text only.
 * FullTellers stay on the current page; the toast offers Go there when they
 * are not already on that stage's work page.
 */
export function notifyElectionStageChanged(
  electionGuid: string,
  newStage: string,
  options?: {
    currentPath?: string;
    navigate?: (path: string) => void;
    isFullTeller?: boolean;
  },
): void {
  const text = i18n.global.t("elections.stageAdvanced", {
    stage: stageLabel(newStage),
  });

  const parsedStage = Object.hasOwn(STAGE_META, newStage)
    ? (newStage as ElectionStage)
    : undefined;
  const destination = parsedStage
    ? getStageWorkPagePath(electionGuid, parsedStage)
    : undefined;
  // Do not import router at module load — electionStore is pulled into suites
  // that mock vue-router without createRouter. History path matches the SPA URL.
  const currentPath =
    options?.currentPath ??
    (typeof window !== "undefined" ? window.location.pathname : "");
  const fullTeller = options?.isFullTeller ?? isFullTeller();
  const showGoThere =
    fullTeller &&
    !!destination &&
    !pathsEqualIgnoreCase(currentPath, destination);

  const navigate =
    options?.navigate ??
    ((path: string) => {
      void import("@/router/router").then(({ router }) => {
        void router.push(path);
      });
    });

  ElMessage({
    message: showGoThere
      ? buildGoThereMessage(text, () => navigate(destination!))
      : text,
    type: "info",
    duration: showGoThere ? STAGE_NOTICE_GO_THERE_MS : STAGE_NOTICE_TEXT_MS,
    showClose: showGoThere,
  });
}
