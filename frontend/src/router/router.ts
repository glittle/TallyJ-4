import { createRouter, createWebHistory } from "vue-router";

import type { RouteLocationNormalized } from "vue-router";

import { secureTokenService } from "../services/secureTokenService";

// PublicLayout is static - needed immediately for all public/voting routes
import PublicLayout from "../layouts/PublicLayout.vue";

// MainLayout is lazy - only authenticated admin users need it
const MainLayout = () => import("../layouts/MainLayout.vue");

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

        meta: { titleKey: "auth.googleCallback.title" },
      },

      {
        path: "voter-auth",

        name: "voter-auth",

        component: () => import("../pages/voting/VoterAuthPage.vue"),

        meta: { titleKey: "auth.voterAuth.title" },
      },

      {
        path: "voter-elections",

        name: "voter-elections",

        component: () => import("../pages/voting/VoterElectionsPage.vue"),

        meta: { titleKey: "voting.elections.title" },
      },

      {
        path: "vote/:electionId",

        name: "voter-ballot",

        component: () => import("../pages/voting/VoterBallotPage.vue"),

        meta: { titleKey: "auth.voterBallot.title" },
      },

      {
        path: "vote-confirmation",

        name: "voter-confirmation",

        component: () => import("../pages/voting/VoterConfirmationPage.vue"),

        meta: { titleKey: "auth.voterConfirmation.title" },
      },

      {
        path: "public/display/:electionGuid",

        name: "public-display",

        component: () => import("../pages/PublicDisplayPage.vue"),

        meta: { titleKey: "nav.publicDisplay" },
      },

      {
        path: "teller-join/:accessCode?/:electionGuid?",

        name: "teller-join",

        component: () => import("../pages/TellerJoinPage.vue"),

        meta: { titleKey: "auth.tellerJoin.title" },
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

        meta: { titleKey: "nav.dashboard" },
      },

      {
        path: "elections",

        redirect: "/dashboard",
      },

      {
        path: "elections/create",

        component: () => import("../pages/elections/CreateElectionPage.vue"),

        meta: { titleKey: "elections.createNew" },
      },

      {
        path: "elections/:id",

        component: () => import("../pages/elections/ElectionDetailPage.vue"),

        meta: { titleKey: "elections.details" },
      },

      {
        path: "elections/:id/edit",

        component: () => import("../pages/elections/EditElectionPage.vue"),

        meta: { titleKey: "elections.edit" },
      },

      {
        path: "elections/:id/people",

        component: () => import("../pages/people/PeopleManagementPage.vue"),

        meta: { titleKey: "people.management" },
      },

      {
        path: "elections/:id/people/import",

        component: () => import("../pages/people/PeopleImportPage.vue"),

        meta: { titleKey: "people.import.title" },
      },

      {
        path: "elections/:id/locations",

        component: () => import("../pages/locations/LocationsListPage.vue"),

        meta: { titleKey: "nav.votingLocations" },
      },

      {
        path: "elections/:id/tellers",

        component: () => import("../pages/tellers/TellersListPage.vue"),

        meta: { titleKey: "nav.tellers" },
      },

      {
        path: "elections/:id/frontdesk",

        component: () => import("../pages/frontdesk/FrontDeskPage.vue"),

        meta: { titleKey: "nav.frontDesk" },
      },

      {
        path: "elections/:id/ballots",

        component: () => import("../pages/ballots/BallotManagementPage.vue"),

        meta: { titleKey: "ballots.management" },
      },

      {
        path: "elections/:id/ballots/cdn-import",

        component: () => import("../pages/ballots/CdnBallotImportPage.vue"),

        meta: { titleKey: "ballots.cdnImport.title" },
      },

      {
        path: "elections/:id/ballots/:ballotId/entry",

        component: () => import("../pages/ballots/BallotEntryPage.vue"),

        meta: { titleKey: "ballots.entryPage" },
      },

      {
        path: "elections/:id/results",

        component: () => import("../pages/results/ResultsPage.vue"),

        meta: { titleKey: "results.title" },
      },

      {
        path: "elections/:id/tally",

        component: () => import("../pages/results/TallyCalculationPage.vue"),

        meta: { titleKey: "results.calculateTally" },
      },

      {
        path: "elections/:id/monitor",

        component: () => import("../pages/results/MonitoringDashboardPage.vue"),

        meta: { titleKey: "results.monitor" },
      },

      {
        path: "elections/:id/tie-management",

        component: () => import("../pages/results/TieManagementPage.vue"),

        meta: { titleKey: "results.tieManagement" },
      },

      {
        path: "elections/:id/presentation",

        component: () => import("../pages/results/PresentationViewPage.vue"),

        meta: { titleKey: "results.presentation" },
      },

      {
        path: "elections/:id/reporting",

        component: () => import("../pages/results/ReportingPage.vue"),

        meta: { titleKey: "results.reporting" },
      },

      {
        path: "profile",

        component: () => import("../pages/ProfilePage.vue"),

        meta: { titleKey: "nav.profile" },
      },

      {
        path: "audit-logs",

        component: () => import("../pages/AuditLogsPage.vue"),

        meta: { titleKey: "nav.auditLogs" },
      },

      {
        path: "super-admin",

        name: "super-admin",

        component: () => import("../pages/SuperAdminDashboardPage.vue"),

        meta: { titleKey: "nav.superAdmin", requiresSuperAdmin: true },
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
    // Only check super admin status for regular users, not tellers

    // Tellers are identified by authMethod="AccessCode" and name="Teller"

    const authData = secureTokenService.getAuthData();

    const isTeller =
      authData.name === "Teller" && authData.authMethod === "AccessCode";

    if (!isTeller) {
      const { useSuperAdminStore } = await import("../stores/superAdminStore");
      const superAdminStore = useSuperAdminStore();

      // Ensure we have checked super admin status

      await superAdminStore.checkSuperAdminStatus();

      if (to.meta.requiresSuperAdmin && !superAdminStore.isSuperAdmin) {
        return "/dashboard";
      }
    } else if (to.meta.requiresSuperAdmin) {
      // Tellers can never be super admins, redirect them

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
