/// <reference types="vitest" />
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { resolve } from "path";

export default defineConfig({
  plugins: [
    vue({
      template: {
        // Public-folder paths like /assets/logo-trans.png must stay literal in tests.
        // Otherwise the compiler rewrites them to invalid file:// URLs on Windows.
        transformAssetUrls: {
          img: [],
        },
      },
    }),
  ],
  publicDir: resolve(__dirname, "public"),
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
  resolve: {
    alias: {
      "@": resolve(__dirname, "./src"),
    },
  },
  // Configure Vitest to handle Element Plus components
  define: {
    global: "globalThis",
  },
});
