import {
  getGuestTellerRedirectPath,
  isGuestTeller,
  isGuestTellerRouteAllowed,
} from "@/domain/guestTellerAccess";
import { useElectionStore } from "@/stores/electionStore";
import { storeToRefs } from "pinia";
import { watch, type WatchStopHandle } from "vue";
import { useRoute, useRouter } from "vue-router";

/**
 * Keeps GuestTellers on the stage-appropriate page when election stage changes
 * over SignalR (or any store update). Lives in the layout so redirects do not
 * depend on the lazy-loaded sidebar menu being mounted.
 */
export function useGuestTellerStageRedirect(): WatchStopHandle {
  const route = useRoute();
  const router = useRouter();
  const electionStore = useElectionStore();
  const { currentStage, currentElection } = storeToRefs(electionStore);

  return watch(
    [currentStage, () => route.path, currentElection],
    () => {
      if (!isGuestTeller()) {
        return;
      }

      const routeGuid =
        typeof route.params.id === "string" ? route.params.id : undefined;
      if (!routeGuid) {
        return;
      }

      // Wait until the store has the election for this route so we do not
      // redirect using the default SettingUp stage before fetch completes.
      const loaded = currentElection.value;
      if (
        !loaded?.electionGuid ||
        loaded.electionGuid.toLowerCase() !== routeGuid.toLowerCase()
      ) {
        return;
      }

      const stage = currentStage.value;
      const destination = getGuestTellerRedirectPath(routeGuid, stage);
      if (
        !isGuestTellerRouteAllowed(route.path, routeGuid, stage) &&
        route.path !== destination
      ) {
        void router.push(destination);
      }
    },
    { immediate: true },
  );
}
