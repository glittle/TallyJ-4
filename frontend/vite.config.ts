import VueI18nPlugin from "@intlify/unplugin-vue-i18n/vite";
import vue from "@vitejs/plugin-vue";
import { execSync } from "node:child_process";
import path, { dirname, resolve } from "node:path";
import { fileURLToPath, URL } from "node:url";
import { visualizer } from "rollup-plugin-visualizer";
import { defineConfig } from "vite";
import viteCompression from "vite-plugin-compression";

// https://vite.dev/config/
export default defineConfig(({ command }) => {
  const projectRoot = dirname(fileURLToPath(import.meta.url));
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
          manualChunks: (id) => {
            // Vendor chunks
            if (id.includes("node_modules")) {
              if (id.includes("element-plus")) {
                return "element-plus";
              }
              if (id.includes("vue-i18n") || id.includes("@intlify")) {
                return "i18n";
              }
              if (id.includes("@microsoft/signalr")) {
                return "signalr";
              }
              if (
                id.includes("axios") ||
                id.includes("localforage") ||
                id.includes("@vueuse")
              ) {
                return "utils";
              }
              // Combine Vue ecosystem packages into vendor to avoid circular dependencies
              return "vendor";
            }

            // API client chunk - separate the large generated SDK
            if (id.includes("/src/api/gen/")) {
              return "api-client";
            }

            // Feature chunks
            if (id.includes("/src/pages/voting/")) {
              return "voting";
            }
            if (
              id.includes("/src/layouts/MainLayout") ||
              id.includes("/src/components/AppHeader") ||
              id.includes("/src/components/AppSidebar")
            ) {
              return "admin-layout";
            }
            if (id.includes("/src/pages/elections/")) {
              return "elections";
            }
            if (id.includes("/src/pages/results/")) {
              return "results";
            }
            if (id.includes("/src/pages/people/")) {
              return "people";
            }
            if (id.includes("/src/pages/ballots/")) {
              return "ballots";
            }
            if (id.includes("/src/pages/SuperAdminDashboardPage")) {
              return "super-admin";
            }
          },
          chunkFileNames: "assets/[name]-[hash].js",
          entryFileNames: "assets/[name]-[hash].js",
          assetFileNames: "assets/[name]-[hash].[ext]",
        },
      },
      chunkSizeWarningLimit: 1500, // Increase warning limit to 1.5MB
      minify: "terser", // Use terser for better compression
      sourcemap: true, // Disable sourcemaps in production
      terserOptions: {
        compress: {
          drop_console: true, // Remove console.log in production
          drop_debugger: true, // Remove debugger statements
        },
      },
      // Optimize assets
      assetsInlineLimit: 4096, // Inline assets smaller than 4kb
      cssCodeSplit: true, // Split CSS into separate chunks
      reportCompressedSize: true, // Report compressed sizes
    },
    publicDir: "public", // Ensure service worker is copied
    define: {
      "process.env.BRANCH_NAME": JSON.stringify(branchName),
      "process.env.COMMIT_HASH": JSON.stringify(commitHash),
    },
  };
});
