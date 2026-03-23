import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import VueI18nPlugin from "@intlify/unplugin-vue-i18n/vite";
import { fileURLToPath, URL } from "node:url";
import { resolve, dirname } from "node:path";
import { visualizer } from "rollup-plugin-visualizer";
import viteCompression from "vite-plugin-compression";
import path from "path";
import simpleGit from "simple-git";

const git = simpleGit();

// https://vite.dev/config/
export default defineConfig(async () => {
  const branchInfo = await git.branch();
  const branchName = branchInfo.current;
  const commitHash = await git.revparse(["--short", "HEAD"], (err, result) => {
    if (err) {
      console.error(err);
      return "";
    }
    return result;
  });

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
    VueI18nPlugin({
      include: [
        // fileURLToPath(new URL("./src/locales/**/*.json", import.meta.url)),
        resolve(
          dirname(fileURLToPath(import.meta.url)),
          "./src/locales/**/*.json",
        ),
      ],
    }),
    // Bundle analyzer - generates stats.html
    visualizer({
      filename: "dist/stats.html",
      open: false,
      gzipSize: true,
      brotliSize: true,
    }),
    // Compression for assets
    viteCompression({
      algorithm: "gzip",
      ext: ".gz",
    }),
    viteCompression({
      algorithm: "brotliCompress",
      ext: ".br",
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
            if (
              id.includes("/vue/") ||
              id.includes("/vue-router/") ||
              id.includes("/pinia/") ||
              id.includes("/@vue/")
            ) {
              return "vue-vendor";
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
            return "vendor";
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
    chunkSizeWarningLimit: 1000, // Increase warning limit to 1MB
    minify: "terser", // Use terser for better compression
    sourcemap: false, // Disable sourcemaps in production
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
