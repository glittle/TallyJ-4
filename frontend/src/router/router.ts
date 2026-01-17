import { createRouter, createWebHistory } from "vue-router";
import HelloWorld from "../pages/HelloWorld.vue";
import LoginPage from "../pages/LoginPage.vue";
import RegisterPage from "../pages/RegisterPage.vue";
import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "../stores/authStore";

const routes = [
  { 
    path: "/", 
    component: HelloWorld,
    meta: { requiresAuth: true }
  },
  { 
    path: "/about", 
    component: HelloWorld,
    meta: { requiresAuth: true }
  },
  {
    path: "/login",
    component: LoginPage,
    meta: { requiresAuth: false }
  },
  {
    path: "/register",
    component: RegisterPage,
    meta: { requiresAuth: false }
  }
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

  const authStore = useAuthStore();
  
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath } };
  }
  
  if ((to.path === '/login' || to.path === '/register') && authStore.isAuthenticated) {
    return '/';
  }

  return true;
});
