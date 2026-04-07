import VueI18nPlugin from "@intlify/unplugin-vue-i18n/vite";
import vue from "@vitejs/plugin-vue";
import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath, URL } from "node:url";
import { visualizer } from "rollup-plugin-visualizer";
import { defineConfig } from "vite";
import devtoolsJson from "vite-plugin-devtools-json";

type ChunkRule = { patterns: string[]; chunk: string };

const vendorChunkRules: ChunkRule[] = [
  { patterns: ["element-plus"], chunk: "vendor-element-plus" },
  { patterns: ["@sentry"], chunk: "vendor-sentry" },
  { patterns: ["vue-i18n", "@intlify"], chunk: "vendor-i18n" },
  { patterns: ["@microsoft/signalr"], chunk: "vendor-signalr" },
  { patterns: ["chart.js", "vue-chartjs"], chunk: "vendor-chartjs" },
  { patterns: ["qrcode"], chunk: "vendor-qrcode" },
  { patterns: ["axios", "localforage", "@vueuse"], chunk: "vendor-utils" },
];

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
    patterns: [
      "/src/layouts/MainLayout",
      "/src/components/AppHeader",
      "/src/components/AppSidebar",
    ],
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
    patterns: ["/src/pages/elections/", "/src/components/elections/"],
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
    patterns: ["/src/pages/locations/", "/src/components/locations/"],
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
          if (
            warning.code === "INVALID_ANNOTATION" &&
            warning.id?.includes("@microsoft/signalr")
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
