import { ElMessage } from "element-plus";

export type NotificationType = "success" | "warning" | "info" | "error";

export interface NotificationOptions {
  title?: string;
  message: string;
  type?: NotificationType;
  duration?: number;
  showClose?: boolean;
  /** Pixels from the top of the viewport. Omit to keep Element Plus's default. */
  offset?: number;
}

export function useNotifications() {
  const computeDuration = (message: string): number => {
    // Base duration on message length (e.g., 300ms per character, with a minimum of 2000ms and a maximum of 10000ms)
    const calculatedDuration = message.length * 300;
    return Math.min(Math.max(calculatedDuration, 2000), 10000);
  };

  const showMessage = (options: NotificationOptions) => {
    const {
      type = options.type || "info",
      message = options.message,
      duration = options.duration ?? computeDuration(message),
      showClose = options.showClose ?? true,
      offset,
    } = options;

    ElMessage({
      message,
      type,
      duration,
      showClose,
      grouping: true, // Group messages to avoid clutter
      ...(typeof offset === "number" ? { offset } : {}),
    });
  };

  // const showNotification = (options: NotificationOptions) => {
  //   const {
  //     title,
  //     message,
  //     type = "info",
  //     duration = 4500,
  //     showClose = true,
  //   } = options;

  //   ElNotification({
  //     title: title || getDefaultTitle(type),
  //     message,
  //     type,
  //     duration,
  //     showClose,
  //   });
  // };

  // const getDefaultTitle = (type: NotificationType): string => {
  //   switch (type) {
  //     case "success":
  //       return t("notification.success");
  //     case "warning":
  //       return t("notification.warning");
  //     case "error":
  //       return t("notification.error");
  //     case "info":
  //     default:
  //       return t("notification.info");
  //   }
  // };

  const showSuccessMessage = (
    message: string,
    duration?: number,
    offset?: number,
  ) => {
    showMessage({ message, type: "success", duration, offset });
  };

  const showErrorMessage = (
    message: string,
    duration?: number,
    offset?: number,
  ) => {
    showMessage({
      message,
      type: "error",
      duration: duration ?? 0,
      showClose: true,
      offset,
    });
  };

  const showWarningMessage = (
    message: string,
    duration?: number,
    offset?: number,
  ) => {
    showMessage({ message, type: "warning", duration, offset });
  };

  const showInfoMessage = (
    message: string,
    duration?: number,
    offset?: number,
  ) => {
    showMessage({ message, type: "info", duration, offset });
  };

  return {
    // showMessage,
    // showNotification,
    showSuccessMessage,
    showErrorMessage,
    showWarningMessage,
    showInfoMessage,
  };
}
