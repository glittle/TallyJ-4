import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const here = dirname(fileURLToPath(import.meta.url));
const frontendSrc = resolve(here, "../..");

function readSrc(rel: string): string {
  return readFileSync(resolve(frontendSrc, rel), "utf8");
}

describe("leftover dark-theme surfaces (#285 follow-up)", () => {
  it("Front Desk registration overlay uses tokens, not frozen white", () => {
    const source = readSrc("components/frontdesk/FrontDeskRegistrationOverlay.vue");
    expect(source).toContain("background: var(--color-orange-50)");
    expect(source).toContain("color: var(--color-text-inverse) !important");
    expect(source).not.toMatch(/color:\s*#fff\b/);
  });

  it("voter ballot and elections rows use semantic 50 tokens", () => {
    const ballot = readSrc("pages/voting/VoterBallotPage.vue");
    const elections = readSrc("pages/voting/VoterElectionsPage.vue");
    expect(ballot).toContain("background-color: var(--color-success-50)");
    expect(ballot).toContain("background-color: var(--color-error-50)");
    expect(ballot).not.toContain("--el-color-success-light-9");
    expect(elections).toContain("background-color: var(--color-warning-50)");
    expect(elections).not.toMatch(/background-color:\s*#fffbe6/);
  });

  it("Profile and guest-teller QR pads use the always-white QR token", () => {
    expect(readSrc("pages/ProfilePage.vue")).toContain(
      "background: var(--color-qr-pad)",
    );
    expect(readSrc("components/common/GuestTellerAccessToggle.vue")).toContain(
      "background: var(--color-qr-pad)",
    );
    expect(readSrc("pages/ProfilePage.vue")).not.toMatch(/background:\s*#fff\b/);
  });

  it("Teller join and CardSkeleton drop frozen light chrome", () => {
    expect(readSrc("pages/TellerJoinPage.vue")).toContain(
      "border: 1px solid var(--el-border-color)",
    );
    expect(readSrc("pages/TellerJoinPage.vue")).not.toContain("#dcdfe6");
    expect(readSrc("components/common/CardSkeleton.vue")).toContain(
      "background: var(--el-fill-color-blank)",
    );
    expect(readSrc("components/common/CardSkeleton.vue")).not.toMatch(
      /background:\s*white/,
    );
  });

  it("warning banners use warning tokens, not cream hex", () => {
    for (const rel of [
      "components/AppSidebar.vue",
      "pages/LandingPage.vue",
      "components/common/LanguageFlagsSelector.vue",
    ]) {
      const source = readSrc(rel);
      expect(source).toContain("background-color: var(--color-warning-50)");
      expect(source).toContain("color: var(--color-warning-700)");
      expect(source).toContain("var(--color-warning-500)");
      expect(source).not.toContain("#fff4e5");
      expect(source).not.toContain("#8c4a00");
      expect(source).not.toContain("#3f2a1a");
    }
  });

  it("audit log filters use the shared fill token", () => {
    const source = readSrc("pages/AuditLogsPage.vue");
    expect(source).toContain("background-color: var(--el-fill-color-light)");
    expect(source).not.toContain("#f5f7fa");
  });

  it("teller inverse text on solid fills uses the inverse token", () => {
    const panel = readSrc("components/ballots/BallotVotesPanel.vue");
    expect(panel).toContain("color: var(--color-text-inverse)");
    expect(panel).not.toMatch(/#fff\b/);
    const stage = readSrc("components/nav/StageControl.vue");
    expect(stage).toContain("var(--color-text-inverse)");
    expect(stage).not.toMatch(/#fff\b/);
    const banner = readSrc("components/common/TestElectionBanner.vue");
    expect(banner).toContain("color: var(--color-text-inverse)");
    expect(banner).not.toMatch(/color:\s*#fff\b/);
  });

  it("ballot import divider is not a frozen light hairline", () => {
    const source = readSrc("pages/ballots/BallotImportPage.vue");
    expect(source).toContain(
      "border-top: 1px solid var(--el-border-color-extra-light)",
    );
    expect(source).not.toContain("#eee");
  });

  it("results tie cards use border/danger tokens", () => {
    const ties = readSrc("pages/results/TieManagementPage.vue");
    const display = readSrc("components/results/TiesDisplay.vue");
    expect(ties).toContain("border: 2px solid var(--el-border-color)");
    expect(ties).toContain("border-color: var(--el-color-danger)");
    expect(ties).not.toContain("#ebeef5");
    expect(display).toContain("border: 2px solid var(--el-color-danger)");
    expect(display).not.toContain("#f56c6c");
  });
});
