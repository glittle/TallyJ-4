import { createRouter, createWebHistory } from "vue-router";
import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useSuperAdminStore } from "../stores/superAdminStore";
import { secureTokenService } from "../services/secureTokenService";

// Layouts - keep these static as they're used frequently
import MainLayout from "../layouts/MainLayout.vue";
import PublicLayout from "../layouts/PublicLayout.vue";

const routes = [
  {
    path: "/",
    component: PublicLayout,
    meta: { requiresAuth: false },
    children: [
      {
        path: "",
        name: "landing",
        component: () => import("../pages/LandingPage.vue"),
      },
      {
        path: "login",
        component: () => import("../pages/LoginPage.vue"),
      },
      {
        path: "register",
        component: () => import("../pages/RegisterPage.vue"),
      },
      {
        path: "auth/google/callback",
        name: "google-callback",
        component: () => import("../pages/GoogleCallbackPage.vue"),
        meta: { title: "Completing Sign In" },
      },
      {
        path: "voter-auth",
        name: "voter-auth",
        component: () => import("../pages/voting/VoterAuthPage.vue"),
        meta: { title: "Voter Authentication" },
      },
      {
        path: "vote/:electionId",
        name: "voter-ballot",
        component: () => import("../pages/voting/VoterBallotPage.vue"),
        meta: { title: "Cast Your Ballot" },
      },
      {
        path: "vote-confirmation",
        name: "voter-confirmation",
        component: () => import("../pages/voting/VoterConfirmationPage.vue"),
        meta: { title: "Vote Confirmed" },
      },
      {
        path: "public/display/:electionGuid",
        name: "public-display",
        component: () => import("../pages/PublicDisplayPage.vue"),
        meta: { title: "Election Results Display" },
      },
    ],
  },
  {
    path: "/",
    component: MainLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: "dashboard",
        component: () => import("../pages/DashboardPage.vue"),
        meta: { title: "Dashboard" },
      },
      {
        path: "elections",
        component: () => import("../pages/elections/ElectionListPage.vue"),
        meta: { title: "Elections" },
      },
      {
        path: "elections/create",
        component: () => import("../pages/elections/CreateElectionPage.vue"),
        meta: { title: "Create Election" },
      },
      {
        path: "elections/:id",
        component: () => import("../pages/elections/ElectionDetailPage.vue"),
        meta: { title: "Election Details" },
      },
      {
        path: "elections/:id/edit",
        component: () => import("../pages/elections/EditElectionPage.vue"),
        meta: { title: "Edit Election" },
      },
      {
        path: "elections/:id/people",
        component: () => import("../pages/people/PeopleManagementPage.vue"),
        meta: { title: "People Management" },
      },
      {
        path: "elections/:id/locations",
        component: () => import("../pages/locations/LocationsListPage.vue"),
        meta: { title: "Voting Locations" },
      },
      {
        path: "elections/:id/tellers",
        component: () => import("../pages/tellers/TellersListPage.vue"),
        meta: { title: "Tellers" },
      },
      {
        path: "elections/:id/frontdesk",
        component: () => import("../pages/frontdesk/FrontDeskPage.vue"),
        meta: { title: "Front Desk" },
      },
      {
        path: "elections/:id/ballots",
        component: () => import("../pages/ballots/BallotManagementPage.vue"),
        meta: { title: "Ballot Management" },
      },
      {
        path: "elections/:id/ballots/import",
        component: () => import("../pages/ballots/BallotImportPage.vue"),
        meta: { title: "Import Ballots" },
      },
      {
        path: "elections/:id/ballots/:ballotId/entry",
        component: () => import("../pages/ballots/BallotEntryPage.vue"),
        meta: { title: "Ballot Entry" },
      },
      {
        path: "elections/:id/results",
        component: () => import("../pages/results/ResultsPage.vue"),
        meta: { title: "Results" },
      },
      {
        path: "elections/:id/tally",
        component: () => import("../pages/results/TallyCalculationPage.vue"),
        meta: { title: "Calculate Tally" },
      },
      {
        path: "elections/:id/monitor",
        component: () => import("../pages/results/MonitoringDashboardPage.vue"),
        meta: { title: "Election Monitor" },
      },
      {
        path: "elections/:id/tie-management",
        component: () => import("../pages/results/TieManagementPage.vue"),
        meta: { title: "Tie Management" },
      },
      {
        path: "elections/:id/presentation",
        component: () => import("../pages/results/PresentationViewPage.vue"),
        meta: { title: "Presentation View" },
      },
      {
        path: "elections/:id/reporting",
        component: () => import("../pages/results/ReportingPage.vue"),
        meta: { title: "Reports" },
      },
      {
        path: "profile",
        component: () => import("../pages/ProfilePage.vue"),
        meta: { title: "Profile" },
      },
      {
        path: "audit-logs",
        component: () => import("../pages/AuditLogsPage.vue"),
        meta: { title: "Audit Logs" },
      },
      {
        path: "super-admin",
        name: "super-admin",
        component: () => import("../pages/SuperAdminDashboardPage.vue"),
        meta: { title: "Super Admin", requiresSuperAdmin: true },
      },
    ],
  },
];

// Create router instance with static routes only
export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
});

router.beforeEach(async (to: RouteLocationNormalized) => {
  const isAuthenticated = secureTokenService.isAuthenticated();

  if (to.meta.requiresAuth && !isAuthenticated) {
    return { path: "/login", query: { redirect: to.fullPath } };
  }

  if (isAuthenticated) {
    const superAdminStore = useSuperAdminStore();
    if (!superAdminStore.checkedStatus) {
      await superAdminStore.checkSuperAdminStatus();
    }

    if (to.meta.requiresSuperAdmin && !superAdminStore.isSuperAdmin) {
      return "/dashboard";
    }
  }

  if (
    (to.path === "/" || to.path === "/login" || to.path === "/register") &&
    isAuthenticated
  ) {
    return "/dashboard";
  }

  return true;
});
