import js from "@eslint/js";
import ts from "typescript-eslint";
import pluginVue from "eslint-plugin-vue";
import globals from "globals";
import eslintPluginPrettierRecommended from "eslint-plugin-prettier/recommended";

export default [
  js.configs.recommended,
  ...ts.configs.recommended,
  ...pluginVue.configs["flat/recommended"],
  eslintPluginPrettierRecommended,

  // Project-specific rules
  {
    rules: {
      // JavaScript-specific
      "no-var": "error",
      "prefer-const": "warn",
      eqeqeq: "error",
      curly: "error",
      singleQuote: "off",
      semicolon: "off",
      eolLast: "off",
      "prettier/prettier": "warn",

      // Vue-specific
      "vue/multi-word-component-names": "off",
      "vue/require-explicit-emits": "error",

      // TypeScript-specific
      "@typescript-eslint/no-explicit-any": "off",
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          args: "all",
          argsIgnorePattern: "^_",
          caughtErrors: "all",
          caughtErrorsIgnorePattern: "^_",
          destructuredArrayIgnorePattern: "^_",
          varsIgnorePattern: "(^_)|(^props$)|(^emit$)", // allow props and emit to be unused (for vue)
          ignoreRestSiblings: true,
        },
      ],
    },
    languageOptions: {
      parserOptions: {
        parser: "@typescript-eslint/parser",
        ecmaFeatures: {
          jsx: true,
        },
      },
      globals: {
        // Allow using process.env from Vite
        process: "writable",
        ...globals.browser,
      },
    },
  },

  // Ignore unnecessary files
  {
    ignores: ["public", "dist", "src/api/generated", "src/api/gen", "*.cjs"],
  },
];
