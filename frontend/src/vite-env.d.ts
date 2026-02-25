/// <reference types="vite/client" />

import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    requiresSuperAdmin?: boolean
    title?: string
  }
}

declare global {
  var pinia: import('pinia').Pinia;
  var router: import('vue-router').Router;
  var i18n: import('vue-i18n').I18n;
  var ElementPlus: typeof import('element-plus');

  namespace NodeJS {
    interface ProcessEnv {
      BRANCH_NAME?: string;
      COMMIT_HASH?: string;
    }
  }
}