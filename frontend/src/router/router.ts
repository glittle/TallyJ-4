import { createRouter, createWebHistory } from "vue-router";
import HelloWorld from "../pages/HelloWorld.vue";
import type { RouteLocationNormalized } from "vue-router";

const routes = [
  { path: "/", component: HelloWorld },
  { path: "/about", component: HelloWorld },
];

// Create router instance with static routes only
export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
});

router.beforeEach(async (to: RouteLocationNormalized) => {
  console.log(
    "Router guard executing for:",
    to.fullPath,
    globalThis.document.location.href
  );

  return true;
});
