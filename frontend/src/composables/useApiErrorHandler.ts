import { useNotifications } from "./useNotifications";
import { useI18n } from "vue-i18n";

export interface ApiError {
  response?: {
    status: number;
    data?: {
      error?: string;
      message?: string;
      errors?: Record<string, string[]>;
    };
  };
  message?: string;
}

export function useApiErrorHandler() {
  const { showErrorMessage } = useNotifications();
  const { t } = useI18n();

  const isApiError = (error: any): error is ApiError => {
    return (
      error &&
      typeof error === "object" &&
      ("response" in error || "message" in error)
    );
  };

  const handleApiError = (error: any, customMessage?: string) => {
    let message = customMessage || t("error.somethingWentWrong");

    if (isApiError(error)) {
      if (error.response) {
        const { status, data } = error.response;

        switch (status) {
          case 400:
            message =
              data?.error || data?.message || t("error.validationError");
            break;
          case 401:
            message = t("error.unauthorized");
            break;
          case 403:
            message = t("error.forbidden");
            break;
          case 404:
            message = t("error.notFound");
            break;
          case 500:
            message = t("error.serverError");
            break;
          default:
            message =
              data?.error || data?.message || t("error.somethingWentWrong");
        }

        // Handle validation errors (field-specific errors)
        if (data?.errors && typeof data.errors === "object") {
          const validationErrors = Object.values(data.errors).flat();
          if (validationErrors.length > 0) {
            message = validationErrors[0] || message;
          }
        }
      } else if (error.message) {
        // Network or other errors
        if (
          error.message.includes("Network Error") ||
          error.message.includes("fetch")
        ) {
          message = t("error.networkError");
        } else {
          message = error.message;
        }
      }
    } else if (error.error) {
      message = t(error.error) || error.error || message;
    }

    showErrorMessage(message);
    return message;
  };

  const handleApiSuccess = (message: string) => {
    // Could be extended to handle different types of success messages
    return message;
  };

  return {
    handleApiError,
    handleApiSuccess,
  };
}
