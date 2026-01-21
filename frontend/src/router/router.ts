import { createRouter, createWebHistory } from "vue-router";
import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "../stores/authStore";

import MainLayout from "../layouts/MainLayout.vue";
import PublicLayout from "../layouts/PublicLayout.vue";

import LoginPage from "../pages/LoginPage.vue";
import RegisterPage from "../pages/RegisterPage.vue";
import DashboardPage from "../pages/DashboardPage.vue";
import ElectionListPage from "../pages/elections/ElectionListPage.vue";
import CreateElectionPage from "../pages/elections/CreateElectionPage.vue";
import ElectionDetailPage from "../pages/elections/ElectionDetailPage.vue";
import ProfilePage from "../pages/ProfilePage.vue";

const routes = [
  {
    path: "/",
    redirect: "/dashboard"
  },
  {
    path: "/",
    component: PublicLayout,
    meta: { requiresAuth: false },
    children: [
      {
        path: "login",
        component: LoginPage
      },
      {
        path: "register",
        component: RegisterPage
      }
    ]
  },
  {
    path: "/",
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: "dashboard",
        component: DashboardPage,
        meta: { title: "Dashboard" }
      },
      {
        path: "elections",
        component: ElectionListPage,
        meta: { title: "Elections" }
      },
      {
        path: "elections/create",
        component: CreateElectionPage,
        meta: { title: "Create Election" }
      },
      {
        path: "elections/:id",
        component: ElectionDetailPage,
        meta: { title: "Election Details" }
      },
      {
        path: "profile",
        component: ProfilePage,
        meta: { title: "Profile" }
      }
    ]
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
