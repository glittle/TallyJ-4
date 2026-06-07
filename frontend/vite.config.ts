import VueI18nPlugin from "@intlify/unplugin-vue-i18n/vite";
import vue from "@vitejs/plugin-vue";
import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, URL } from "node:url";
import { visualizer } from "rollup-plugin-visualizer";
import { defineConfig } from "vite";
import devtoolsJson from "vite-plugin-devtools-json";

// ---------------------------------------------------------------------------
// Build-time guard for language support
//
// `npm run build` (and the Azure front-end pipeline) must run merge-locales
// first. The resulting src/locales/bundled/*.json files (gitignored) are
// required so that:
//   - Vite's manualChunks produces locale-*.js chunks
//   - locales/index.ts sets useBundled=true and the import.meta.glob maps
//     have entries for dynamic loading via setLocale()
//   - LanguageFlagsSelector on the landing page (and LanguageSelector elsewhere)
//     can actually fetch and apply non-English translations.
//
// Without the bundled files a bare `npx vite build` produces a dist where
// clicking any flag makes *no* network request (no chunk exists) and the
// UI stays in English (only "common" strings are present).
//
// This guard makes the mistake fail fast and loudly in CI or manual deploys.
// ---------------------------------------------------------------------------
const __dirnameForConfig = path.dirname(fileURLToPath(import.meta.url));
const bundledDir = path.join(__dirnameForConfig, "src", "locales", "bundled");
const enBundledPath = path.join(bundledDir, "en.json");

const isViteBuild =
  process.argv.some((arg) => arg.includes("build")) ||
  process.env.NODE_ENV === "production";

if (isViteBuild && !fs.existsSync(enBundledPath)) {
  throw new Error(
    "[TallyJ] Production `vite build` requires src/locales/bundled/*.json (produced by `npm run merge-locales`).\n" +
      'Run "npm run build" (not bare "npx vite build").\n' +
      "bundled/ is .gitignored. This is why language flags worked in dev + local preview but not on the UAT pipeline build.",
  );
}

type ChunkRule = { patterns: string[]; chunk: string };

const vendorChunkRules: ChunkRule[] = [
  { patterns: ["element-plus"], chunk: "vendor-element-plus" },
  { patterns: ["@sentry"], chunk: "vendor-sentry" },
  { patterns: ["vue-i18n", "@intlify"], chunk: "vendor-i18n" },
  { patterns: ["@microsoft/signalr"], chunk: "vendor-signalr" },
  { patterns: ["chart.js", "vue-chartjs"], chunk: "vendor-chartjs" },
  { patterns: ["qrcode"], chunk: "vendor-qrcode" },
];

// ---------------------------------------------------------------------------
// Manual chunking strategy aligned with the three main user audiences:
//
// 1. Online voters / public users
//    - Only ever load PublicLayout + voting routes + the "voting" chunk.
//    - Should stay completely isolated from all MainLayout / election management code.
//
// 2. Tellers (assistant + full) + election officials
//    - Load the authenticated shell (MainLayout + auth-nav) + common election features.
//    - Individual management areas (people, ballots, frontdesk, locations, tellers,
//      results/tally, reporting, etc.) are loaded dynamically as separate chunks
//      only when the user navigates into those sections.
//    - The heavy StageGroupedSidebarMenu (many icons + full STAGE_PAGES definition)
//      is lazy-loaded via defineAsyncComponent inside AppSidebar so it is not paid
//      until the user actually enters an election context.
//
// 3. Super admins
//    - Everything from (2) plus the isolated "super-admin" chunk.
//    - The superAdminStore is now explicitly routed to the super-admin chunk.
//
// The goal is a small initial payload for voters, a reasonable base + on-demand
// feature bundles for tellers/officials, and isolation of the privileged super-admin tools.
// ---------------------------------------------------------------------------
const srcChunkRules: ChunkRule[] = [
  { patterns: ["/src/api/gen/"], chunk: "api-client" },
  {
    patterns: [
      "/src/pages/voting/",
      "/src/stores/onlineVotingStore",
      "/src/services/onlineVotingService",
      "/src/services/voteService",
      "/src/components/auth/",
    ],
    chunk: "voting",
  },
  {
    // Heavy election navigation menu (many icons + full STAGE_PAGES definition).
    // Even though AppSidebar lazy-loads it via defineAsyncComponent, we must
    // explicitly assign it to the "elections" chunk here; otherwise the broad
    // "/src/components/nav/" rule below would force it into the early "auth-nav"
    // chunk and defeat the lazy-load benefit for tellers/officials.
    patterns: ["/src/components/nav/StageGroupedSidebarMenu"],
    chunk: "elections",
  },
  {
    // Common authenticated navigation and UI pieces (sidebar menu is lazy-loaded,
    // so this mainly catches the lightweight shell + header bits).
    patterns: [
      "/src/components/nav/",
      "/src/components/AppHeader",
      "/src/components/AppSidebar",
    ],
    chunk: "auth-nav",
  },
  {
    // Keep the core layout shell as its own chunk. AppHeader and AppSidebar
    // (especially the heavy StageGroupedSidebarMenu which is now lazy) are
    // allowed to be pulled into feature chunks or a lighter auth shell.
    // This helps keep the base payload smaller for tellers and election managers.
    patterns: ["/src/layouts/MainLayout"],
    chunk: "admin-layout",
  },
  {
    patterns: [
      "/src/pages/ballots/",
      "/src/components/ballots/",
      "/src/stores/ballotStore.",
      "/src/services/ballotService",
    ],
    chunk: "ballots",
  },
  {
    patterns: [
      "/src/pages/results/",
      "/src/components/results/",
      "/src/components/charts/",
      "/src/stores/resultStore",
      "/src/services/resultService",
      "/src/services/reportService",
    ],
    chunk: "results",
  },
  {
    patterns: [
      "/src/pages/elections/",
      "/src/components/elections/",
      "/src/pages/DashboardPage",
      "/src/pages/ProfilePage",
    ],
    chunk: "elections",
  },
  {
    patterns: [
      "/src/pages/people/",
      "/src/components/people/",
      "/src/stores/peopleStore.",
      "/src/services/peopleService",
      "/src/services/peopleImportService",
    ],
    chunk: "people",
  },
  {
    patterns: [
      "/src/pages/locations/",
      "/src/components/locations/",
      "/src/stores/locationStore",
    ],
    chunk: "locations",
  },
  {
    patterns: [
      "/src/pages/tellers/",
      "/src/components/tellers/",
      "/src/stores/tellerStore",
      "/src/services/tellerService",
    ],
    chunk: "tellers",
  },
  {
    patterns: ["/src/pages/frontdesk/", "/src/services/frontDeskService"],
    chunk: "frontdesk",
  },
  {
    patterns: [
      "/src/pages/SuperAdminDashboardPage",
      "/src/services/superAdminService",
      "/src/stores/superAdminStore",
    ],
    chunk: "super-admin",
  },
  {
    patterns: [
      "/src/pages/AuditLogsPage",
      "/src/stores/auditLogStore",
      "/src/services/auditLogService",
    ],
    chunk: "audit",
  },
  {
    patterns: [
      "/src/pages/PublicDisplayPage",
      "/src/stores/publicStore",
      "/src/services/publicService",
    ],
    chunk: "public-display",
  },
];

function matchChunkRule(n: string, rules: ChunkRule[]): string | undefined {
  return rules.find((r) => r.patterns.some((p) => n.includes(p)))?.chunk;
}

function getManualChunks(id: string): string | undefined {
  const n = id.replaceAll("\\", "/");

  if (n.includes("node_modules")) {
    return matchChunkRule(n, vendorChunkRules) ?? "vendor";
  }

  const bundledLocale = /\/locales\/bundled\/(\w+)\.json/.exec(n);
  if (bundledLocale) {
    return `locale-${bundledLocale[1]}`;
  }

  const individualLocale = /\/locales\/([a-z]{2})\/\w+\.json/.exec(n);
  if (individualLocale && individualLocale[1] !== "en") {
    return `locale-${individualLocale[1]}-dev`;
  }

  return matchChunkRule(n, srcChunkRules);
}

// https://vite.dev/config/
export default defineConfig(() => {
  const branchName = execSync("git rev-parse --abbrev-ref HEAD", {
    encoding: "utf-8",
  }).trim();
  const commitHash = execSync("git rev-parse --short HEAD", {
    encoding: "utf-8",
  }).trim();

  return {
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url)),
        vue: path.resolve(__dirname, "node_modules/vue"),
      },
    },
    server: {
      port: 8095,
      hmr: {
        port: 8095,
      },
      // Proxy /clientEnv.json during `npm run dev` to the real backend
      proxy: {
        "/clientEnv.json": {
          target: process.env.VITE_API_TARGET || "http://localhost:5016",
          changeOrigin: true,
        },
      },
    },
    preview: {
      port: 4173,
      // Proxy /clientEnv.json during `npm run preview` to the real backend
      proxy: {
        "/clientEnv.json": {
          target: process.env.VITE_API_TARGET || "http://localhost:5016",
          changeOrigin: true,
        },
      },
    },
    plugins: [
      vue(),
      devtoolsJson(),
      VueI18nPlugin({}),
      // Bundle analyzer - generates stats.html
      visualizer({
        filename: "dist/stats.html",
        open: false,
        gzipSize: true,
        brotliSize: true,
      }),
    ],
    build: {
      rollupOptions: {
        onwarn(warning, warn) {
          // Suppress only for third-party code (harmless annotation issues from vueuse, signalr, etc.).
          // Keep app-code warnings visible so they can be actioned.
          if (
            warning.code === "INVALID_ANNOTATION" &&
            warning.id?.includes("node_modules")
          ) {
            return;
          }
          warn(warning);
        },
        output: {
          manualChunks: getManualChunks,
          chunkFileNames: "assets/[name]-[hash].js",
          entryFileNames: "assets/[name]-[hash].js",
          assetFileNames: "assets/[name]-[hash].[ext]",
        },
      },
      chunkSizeWarningLimit: 1500,
      sourcemap: true,
      // Optimize assets
      assetsInlineLimit: 4096, // Inline assets smaller than 4kb
      cssCodeSplit: true, // Split CSS into separate chunks
      reportCompressedSize: true, // Report compressed sizes
    },
    esbuild: {
      drop: ["console", "debugger"],
    },
    publicDir: "public",
    define: {
      "process.env.BRANCH_NAME": JSON.stringify(branchName),
      "process.env.COMMIT_HASH": JSON.stringify(commitHash),
    },
  };
});
