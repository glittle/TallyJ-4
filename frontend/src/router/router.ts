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
import PeopleManagementPage from "../pages/people/PeopleManagementPage.vue";
import BallotManagementPage from "../pages/ballots/BallotManagementPage.vue";
import ResultsPage from "../pages/results/ResultsPage.vue";
import TallyCalculationPage from "../pages/results/TallyCalculationPage.vue";
import MonitoringDashboardPage from "../pages/results/MonitoringDashboardPage.vue";
import TieManagementPage from "../pages/results/TieManagementPage.vue";
import PresentationViewPage from "../pages/results/PresentationViewPage.vue";
import ReportingPage from "../pages/results/ReportingPage.vue";
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
        path: "elections/:id/people",
        component: PeopleManagementPage,
        meta: { title: "People Management" }
      },
      {
        path: "elections/:id/ballots",
        component: BallotManagementPage,
        meta: { title: "Ballot Management" }
      },
      {
        path: "elections/:id/results",
        component: ResultsPage,
        meta: { title: "Results" }
      },
      {
        path: "elections/:id/tally",
        component: TallyCalculationPage,
        meta: { title: "Calculate Tally" }
      },
      {
        path: "elections/:id/monitor",
        component: MonitoringDashboardPage,
        meta: { title: "Election Monitor" }
      },
      {
        path: "elections/:id/tie-management",
        component: TieManagementPage,
        meta: { title: "Tie Management" }
      },
      {
        path: "elections/:id/presentation",
        component: PresentationViewPage,
        meta: { title: "Presentation View" }
      },
      {
        path: "elections/:id/reporting",
        component: ReportingPage,
        meta: { title: "Reports" }
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
