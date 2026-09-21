import { i18n } from "@/locales";

export interface ApiError {
  title?: string;
  error?: string;
  message?: string;
  attempts?: number;
  errors?: Record<string, string[]>;
  status?: number;
  response?: {
    status: number;
    data?: {
      title?: string;
      error?: string;
      message?: string;
      attempts?: number;
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
    for (const [, messages] of Object.entries(apiError.errors)) {
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
      for (const [, messages] of Object.entries(data.errors)) {
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

const namedNumberSuffix = /^(.*):(\d+)$/;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function readAttemptsField(value: unknown): number | undefined {
  if (!isRecord(value) || typeof value.attempts !== "number") {
    return undefined;
  }
  return value.attempts;
}

/// Remaining OTP attempts from a hey-api 400 body or an axios-shaped wrapper.
export function extractApiErrorAttempts(error: unknown): number | undefined {
  if (!isRecord(error)) {
    return undefined;
  }
  const fromBody = readAttemptsField(error);
  if (fromBody !== undefined) {
    return fromBody;
  }

  const response = error.response;
  return isRecord(response) ? readAttemptsField(response.data) : undefined;
}

/// Split a stable key from an optional `:N` suffix (legacy invalidCode payload).
export function parsePhraseKey(message: string): {
  key: string;
  attempts?: number;
} {
  const key = message?.trim() ?? "";
  const match = namedNumberSuffix.exec(key);
  if (!match) {
    return { key };
  }

  const { te } = i18n.global;
  const prefix = match[1] ?? "";
  if (!te(prefix)) {
    return { key };
  }

  return { key: prefix, attempts: Number(match[2]) };
}

/// When the API returns an i18n phrase key (e.g. elections.finalizedWriteBlocked),
/// translate it. Leave ordinary English exception text unchanged.
export function translateIfPhraseKey(
  message: string,
  named?: Record<string, unknown>,
): string {
  const parsed = parsePhraseKey(message);
  if (!parsed.key) {
    return message;
  }

  const { t, te } = i18n.global;
  if (!te(parsed.key)) {
    return message;
  }

  const attempts = named?.attempts ?? parsed.attempts;
  return String(
    attempts === undefined ? t(parsed.key) : t(parsed.key, { attempts }),
  );
}

export function resolveUserFacingApiError(
  error: unknown,
  fallback: string,
): string {
  const raw = extractApiErrorMessage(error);
  if (!raw || raw === "An unknown error occurred") {
    return fallback;
  }

  const attempts = extractApiErrorAttempts(error);
  return translateIfPhraseKey(
    raw,
    attempts === undefined ? undefined : { attempts },
  );
}
