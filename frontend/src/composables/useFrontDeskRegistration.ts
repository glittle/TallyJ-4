import type {
  FrontDeskVoterDto,
  RegistrationHistoryEntryDto,
} from "@/types/FrontDesk";
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
import { formatRegistrationHistoryDetails } from "@/utils/formatRegistrationHistory";
import { ElMessageBox } from "element-plus";
import { computed, nextTick, ref, type ComputedRef, type Ref } from "vue";

export type FrontDeskDialogButton = {
  value: string;
  label: string;
  key: string;
  isVotingMethod: boolean;
  isUnregister: boolean;
  isClose: boolean;
};

export type RegistrationTypeOption = {
  value: string;
  label: string;
  key: string;
  isVotingMethod: boolean;
};

type Translate = (key: string, values?: Record<string, unknown>) => string;

export type UseFrontDeskRegistrationOptions = {
  electionGuid: Ref<string>;
  hasActiveTeller: Ref<boolean> | ComputedRef<boolean>;
  electionFlags: Ref<string[]> | ComputedRef<string[]>;
  registrationTypes: ComputedRef<RegistrationTypeOption[]>;
  selectedVoter: Ref<FrontDeskVoterDto | null>;
  searchInputRef: Ref<{ focus?: () => void; $el?: HTMLElement } | null>;
  registrationOverlayRef: Ref<{
    focus?: () => void;
    querySelector?: (selector: string) => Element | null;
  } | null>;
  checkInVoter: (
    guid: string,
    dto: {
      personGuid: string;
      votingMethod: string;
      votingLocationGuid?: string;
      teller1?: string;
      teller2?: string;
    },
  ) => Promise<FrontDeskVoterDto>;
  unregisterVoter: (
    guid: string,
    dto: { personGuid: string; reason: string },
  ) => Promise<FrontDeskVoterDto>;
  savePersonFlags: (
    guid: string,
    dto: { personGuid: string; flags: string },
  ) => Promise<FrontDeskVoterDto>;
  t: Translate;
  showSuccessMessage: (msg: string) => void;
  showErrorMessage: (msg: string) => void;
};

/**
 * Front-desk registration overlay: dialog buttons, keyboard focus, check-in/flags/unregister.
 */
export function useFrontDeskRegistration(
  options: UseFrontDeskRegistrationOptions,
) {
  const showRegistrationButtons = ref(false);
  const selectedButtonIndex = ref(0);
  const pendingVotingMethod = ref<string | null>(null);
  const checkInInProgress = ref(false);
  const pendingCheckInPersonGuid = ref<string | null>(null);

  const dialogButtons = computed((): FrontDeskDialogButton[] => {
    const buttons: FrontDeskDialogButton[] = [];

    if (!options.selectedVoter.value?.isCheckedIn) {
      options.registrationTypes.value.forEach((type) => {
        buttons.push({
          value: type.value,
          label: type.label,
          key: "",
          isVotingMethod: true,
          isUnregister: false,
          isClose: false,
        });
      });
    }

    options.electionFlags.value.forEach((flag: string) => {
      buttons.push({
        value: flag,
        label: flag,
        key: "",
        isVotingMethod: false,
        isUnregister: false,
        isClose: false,
      });
    });

    if (options.selectedVoter.value?.isCheckedIn) {
      buttons.push({
        value: "__unregister__",
        label: options.t("frontDesk.dialog.unregister"),
        key: "",
        isVotingMethod: false,
        isUnregister: true,
        isClose: false,
      });
    }

    buttons.push({
      value: "__close__",
      label: options.t("common.close"),
      key: "",
      isVotingMethod: false,
      isUnregister: false,
      isClose: true,
    });

    buttons.forEach((button, index) => {
      button.key = String(index + 1);
    });

    return buttons;
  });

  function focusRegistrationOverlay() {
    nextTick(() => {
      options.registrationOverlayRef.value?.focus?.();
    });
  }

  function getInitialDialogButtonIndex(): number {
    if (
      !options.hasActiveTeller.value ||
      (options.selectedVoter.value?.isCheckedIn &&
        options.electionFlags.value.length === 0)
    ) {
      const closeIndex = dialogButtons.value.findIndex(
        (button) => button.isClose,
      );
      return closeIndex >= 0 ? closeIndex : 0;
    }
    return 0;
  }

  function isDialogButtonActionable(button: FrontDeskDialogButton): boolean {
    if (button.isClose) {
      return true;
    }
    return options.hasActiveTeller.value;
  }

  function isDialogButtonFocusable(button: FrontDeskDialogButton): boolean {
    if (!isDialogButtonActionable(button)) {
      return false;
    }
    return !(button.isClose && checkInInProgress.value);
  }

  function getNextDialogButtonIndex(
    currentIndex: number,
    direction: 1 | -1,
  ): number {
    const buttons = dialogButtons.value;
    if (buttons.length === 0) {
      return 0;
    }

    let nextIndex = currentIndex;
    for (let step = 0; step < buttons.length; step++) {
      nextIndex =
        direction === 1
          ? (nextIndex + 1) % buttons.length
          : (nextIndex - 1 + buttons.length) % buttons.length;
      const button = buttons[nextIndex];
      if (button && isDialogButtonFocusable(button)) {
        return nextIndex;
      }
    }

    return currentIndex;
  }

  function getDialogButtonIndex(value: string): number {
    return dialogButtons.value.findIndex((button) => button.value === value);
  }

  function getDialogButtonKey(value: string): string {
    return (
      dialogButtons.value.find((button) => button.value === value)?.key ?? ""
    );
  }

  function isDialogButtonKeyboardFocused(value: string): boolean {
    if (pendingVotingMethod.value === value) {
      return false;
    }
    return getDialogButtonIndex(value) === selectedButtonIndex.value;
  }

  function focusRegistrationButton() {
    nextTick(() => {
      const overlay = options.registrationOverlayRef.value;
      const selectedButton = dialogButtons.value[selectedButtonIndex.value];
      if (
        !overlay ||
        !selectedButton ||
        !isDialogButtonFocusable(selectedButton)
      ) {
        return;
      }

      const target = overlay.querySelector?.(
        `[data-dialog-button="${selectedButton.value}"]`,
      ) as HTMLButtonElement | null;
      target?.focus();
    });
  }

  function openRegistrationDialog() {
    showRegistrationButtons.value = true;
    selectedButtonIndex.value = getInitialDialogButtonIndex();
    focusRegistrationOverlay();
    focusRegistrationButton();
  }

  function closeRegistrationDialog() {
    showRegistrationButtons.value = false;
    selectedButtonIndex.value = 0;
    pendingVotingMethod.value = null;
    checkInInProgress.value = false;
    pendingCheckInPersonGuid.value = null;

    nextTick(() => {
      options.searchInputRef.value?.focus?.();
    });
  }

  function getVotingMethodLabel(method?: string): string {
    const match = options.registrationTypes.value.find(
      (type) => type.value === method,
    );
    return match?.label ?? method ?? options.t("frontDesk.common.dash");
  }

  function handleRegistrationKeydown(event: KeyboardEvent) {
    const buttons = dialogButtons.value;

    if (event.key === "ArrowLeft") {
      event.preventDefault();
      selectedButtonIndex.value = getNextDialogButtonIndex(
        selectedButtonIndex.value,
        -1,
      );
      focusRegistrationButton();
    } else if (event.key === "ArrowRight") {
      event.preventDefault();
      selectedButtonIndex.value = getNextDialogButtonIndex(
        selectedButtonIndex.value,
        1,
      );
      focusRegistrationButton();
    } else if (event.key === "Tab") {
      event.preventDefault();
      selectedButtonIndex.value = getNextDialogButtonIndex(
        selectedButtonIndex.value,
        event.shiftKey ? -1 : 1,
      );
      focusRegistrationButton();
    } else if (event.key === "Enter") {
      event.preventDefault();
      if (checkInInProgress.value) {
        return;
      }
      const selectedButton = buttons[selectedButtonIndex.value];
      if (selectedButton && isDialogButtonActionable(selectedButton)) {
        void handleButtonClick(selectedButton);
      }
    } else if (event.key >= "1" && event.key <= "9") {
      event.preventDefault();
      const index = parseInt(event.key) - 1;
      if (index >= 0 && index < buttons.length) {
        const button = buttons[index];
        if (!button) {
          options.showErrorMessage(
            options.t("frontDesk.messages.invalidButton", {
              index: index + 1,
            }),
          );
          return;
        }
        if (!isDialogButtonActionable(button)) {
          return;
        }
        void handleButtonClick(button);
      }
    }
  }

  function clickDialogButton(value: string) {
    const button = dialogButtons.value.find(
      (dialogButton) => dialogButton.value === value,
    );
    if (button) {
      void handleButtonClick(button);
    }
  }

  async function confirmCheckIn(votingMethod: string) {
    if (
      !options.selectedVoter.value ||
      checkInInProgress.value ||
      !options.hasActiveTeller.value
    ) {
      return;
    }

    const personGuid = options.selectedVoter.value.personGuid;

    pendingVotingMethod.value = votingMethod;
    checkInInProgress.value = true;
    pendingCheckInPersonGuid.value = personGuid;

    try {
      await options.checkInVoter(options.electionGuid.value, {
        personGuid,
        votingMethod,
        ...getActiveTellerPayload(),
        votingLocationGuid: undefined,
      });
    } catch (err: unknown) {
      pendingVotingMethod.value = null;
      checkInInProgress.value = false;
      pendingCheckInPersonGuid.value = null;
      const message =
        err instanceof Error
          ? err.message
          : options.t("frontDesk.errors.checkIn");
      options.showErrorMessage(
        message || options.t("frontDesk.errors.checkIn"),
      );
    }
  }

  async function handleButtonClick(button: FrontDeskDialogButton) {
    if (!options.selectedVoter.value || checkInInProgress.value) {
      return;
    }
    if (!isDialogButtonActionable(button)) {
      return;
    }

    if (button.isClose) {
      if (!checkInInProgress.value) {
        closeRegistrationDialog();
      }
    } else if (button.isUnregister) {
      await handleUnregisterSelected();
    } else if (button.isVotingMethod) {
      pendingVotingMethod.value = button.value;
      await confirmCheckIn(button.value);
    } else {
      await toggleFlag(button.value);
    }
  }

  function hasFlag(voter: FrontDeskVoterDto, flag: string): boolean {
    if (!voter.flags) {
      return false;
    }
    const flags = voter.flags.split(",").map((f) => f.trim());
    return flags.includes(flag);
  }

  async function toggleFlag(flag: string) {
    if (!options.selectedVoter.value || !options.hasActiveTeller.value) {
      return;
    }

    const currentFlags = options.selectedVoter.value.flags
      ? options.selectedVoter.value.flags
          .split(",")
          .map((f) => f.trim())
          .filter(Boolean)
      : [];

    const hasCurrentFlag = currentFlags.includes(flag);

    if (hasCurrentFlag) {
      try {
        await ElMessageBox.confirm(
          options.t("frontDesk.confirm.removeFlag.message", {
            flag,
            name: options.selectedVoter.value.fullName,
          }),
          options.t("frontDesk.confirm.removeFlag.title"),
          {
            confirmButtonText: options.t(
              "frontDesk.confirm.removeFlag.confirm",
            ),
            cancelButtonText: options.t("common.cancel"),
            type: "warning",
          },
        );
      } catch {
        return;
      }

      const updatedFlags = currentFlags.filter((f) => f !== flag);
      await updatePersonFlags(updatedFlags);
    } else {
      currentFlags.push(flag);
      await updatePersonFlags(currentFlags);
    }
  }

  async function updatePersonFlags(flags: string[]) {
    if (!options.selectedVoter.value) {
      return;
    }

    try {
      const updated = await options.savePersonFlags(
        options.electionGuid.value,
        {
          personGuid: options.selectedVoter.value.personGuid,
          flags: flags.join(", "),
        },
      );
      options.selectedVoter.value = updated;
      options.showSuccessMessage(options.t("frontDesk.messages.flagsUpdated"));
    } catch (err: unknown) {
      const message =
        err instanceof Error
          ? err.message
          : options.t("frontDesk.errors.updateFlags");
      options.showErrorMessage(
        message || options.t("frontDesk.errors.updateFlags"),
      );
    }
  }

  async function handleUnregister(voter: FrontDeskVoterDto) {
    try {
      await ElMessageBox.confirm(
        options.t("frontDesk.confirm.unregister.message", {
          name: voter.fullName,
        }),
        options.t("frontDesk.confirm.unregister.title"),
        {
          confirmButtonText: options.t("frontDesk.confirm.unregister.confirm"),
          cancelButtonText: options.t("common.cancel"),
          type: "warning",
        },
      );

      const updated = await options.unregisterVoter(
        options.electionGuid.value,
        {
          personGuid: voter.personGuid,
          reason: options.t("frontDesk.unregisterReason"),
        },
      );
      options.selectedVoter.value = updated;

      options.showSuccessMessage(options.t("frontDesk.messages.unregistered"));
      return true;
    } catch (err: unknown) {
      if (err !== "cancel") {
        const message =
          err instanceof Error
            ? err.message
            : options.t("frontDesk.errors.unregister");
        options.showErrorMessage(
          message || options.t("frontDesk.errors.unregister"),
        );
      }
      return false;
    }
  }

  async function handleUnregisterSelected() {
    if (!options.selectedVoter.value || !options.hasActiveTeller.value) {
      return;
    }

    const unregistered = await handleUnregister(options.selectedVoter.value);
    if (unregistered) {
      selectedButtonIndex.value = 0;
      focusRegistrationOverlay();
    }
  }

  function formatTime(time?: string): string {
    if (!time) {
      return "";
    }
    return new Date(time).toLocaleString();
  }

  function formatTimeline(entry: RegistrationHistoryEntryDto): string {
    return formatRegistrationHistoryDetails(entry, {
      t: options.t,
      getVotingMethodLabel,
    });
  }

  return {
    showRegistrationButtons,
    selectedButtonIndex,
    pendingVotingMethod,
    checkInInProgress,
    pendingCheckInPersonGuid,
    dialogButtons,
    openRegistrationDialog,
    closeRegistrationDialog,
    focusRegistrationButton,
    getInitialDialogButtonIndex,
    handleRegistrationKeydown,
    clickDialogButton,
    getDialogButtonKey,
    isDialogButtonKeyboardFocused,
    hasFlag,
    getVotingMethodLabel,
    handleUnregisterSelected,
    formatTime,
    formatTimeline,
  };
}
