import {
  authService,
  type OfficerSignInCapabilities,
  type VoterSignInCapabilities,
} from "@/services/authService";
import { ref } from "vue";

export type { OfficerSignInCapabilities, VoterSignInCapabilities };

const emptyVoters = (): VoterSignInCapabilities => ({
  google: false,
  facebook: false,
  kakao: false,
  telegram: false,
  emailCode: false,
  phoneCode: false,
  sms: false,
  voice: false,
  whatsApp: false,
  directCode: false,
});

export function useSignInCapabilities() {
  const officers = ref<OfficerSignInCapabilities | null>(null);
  const voters = ref<VoterSignInCapabilities | null>(null);
  const loaded = ref(false);

  async function load() {
    try {
      const data = await authService.getSignInCapabilities();
      officers.value = data.officers;
      voters.value = data.voters;
    } catch {
      officers.value = {
        local: true,
        google: false,
        facebook: false,
        kakao: false,
        telegram: false,
      };
      voters.value = emptyVoters();
    } finally {
      loaded.value = true;
    }
  }

  return { officers, voters, loaded, load };
}
