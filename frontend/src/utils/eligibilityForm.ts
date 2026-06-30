/** Sentinel for fully eligible — Element Plus treats null/empty as unselected. */
export const ELIGIBLE_REASON_VALUE = "__eligible__";

export function toApiEligibility(value?: string | null): string | undefined {
  if (!value || value === ELIGIBLE_REASON_VALUE) {
    return undefined;
  }

  return value;
}

export function toFormEligibility(value?: string | null): string {
  return value ?? ELIGIBLE_REASON_VALUE;
}