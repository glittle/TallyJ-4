export interface ApiError {
  title?: string;
  error?: string;
  message?: string;
  errors?: Record<string, string[]>;
  status?: number;
  response?: {
    status: number;
    data?: {
      title?: string;
      error?: string;
      message?: string;
      errors?: Record<string, string[]>;
    };
  };
}

export function extractApiErrorMessage(error: any): string {
  if (!error) {
    return "An unknown error occurred";
  }

  const apiError = error as ApiError;

  if (apiError.errors && typeof apiError.errors === "object") {
    const validationErrors: string[] = [];
    for (const [field, messages] of Object.entries(apiError.errors)) {
      if (Array.isArray(messages)) {
        validationErrors.push(...messages);
      }
    }
    if (validationErrors.length > 0) {
      return validationErrors.join("; ");
    }
  }

  if (apiError.title) {
    return apiError.title;
  }

  if (apiError.error) {
    return apiError.error;
  }

  if (apiError.message) {
    return apiError.message;
  }

  if (apiError.response?.data) {
    const { data } = apiError.response;

    if (data.errors && typeof data.errors === "object") {
      const validationErrors: string[] = [];
      for (const [field, messages] of Object.entries(data.errors)) {
        if (Array.isArray(messages)) {
          validationErrors.push(...messages);
        }
      }
      if (validationErrors.length > 0) {
        return validationErrors.join("; ");
      }
    }

    if (data.title) {
      return data.title;
    }

    if (data.error) {
      return data.error;
    }

    if (data.message) {
      return data.message;
    }
  }

  return "An unknown error occurred";
}
