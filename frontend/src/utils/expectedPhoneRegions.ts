const ISO_REGION = /^[A-Za-z]{2}$/;
const NOT_ISO_CODES = new Set(["UK"]);
export const EXPECTED_PHONE_REGIONS_MAX_LENGTH = 80;

export type ExpectedPhoneRegionsError = "tooLong" | "invalid";

/**
 * Empty is valid. USA and Canada fail the two-letter shape. UK is two letters
 * but is not an ISO code (the code is GB).
 */
export function expectedPhoneRegionsError(
  value: string | null | undefined,
): ExpectedPhoneRegionsError | null {
  if (value === null || value === undefined || value.trim() === "") {
    return null;
  }
  if (value.length > EXPECTED_PHONE_REGIONS_MAX_LENGTH) {
    return "tooLong";
  }

  const tokens = value.split(/[,;\s]+/).filter(Boolean);
  for (const token of tokens) {
    if (!ISO_REGION.test(token) || NOT_ISO_CODES.has(token.toUpperCase())) {
      return "invalid";
    }
  }
  return null;
}

export function expectedPhoneRegionsRule(t: (key: string) => string): {
  validator: (
    rule: unknown,
    value: unknown,
    callback: (error?: Error) => void,
  ) => void;
  trigger: Array<"change" | "blur" | "input">;
} {
  return {
    validator: (_rule, value, callback) => {
      const error = expectedPhoneRegionsError(
        typeof value === "string" ? value : "",
      );
      if (error === "tooLong") {
        callback(new Error(t("elections.form.expectedPhoneRegionsTooLong")));
        return;
      }
      if (error === "invalid") {
        callback(new Error(t("elections.form.expectedPhoneRegionsInvalid")));
        return;
      }
      callback();
    },
    trigger: ["change", "blur", "input"],
  };
}
