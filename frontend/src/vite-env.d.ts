/// <reference types="vite/client" />

declare global {
  var pinia: import('pinia').Pinia;
  var router: import('vue-router').Router;
  var i18n: import('vue-i18n').I18n;
  var ElementPlus: typeof import('element-plus');
}