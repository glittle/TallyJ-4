/**
 * Fixed at 6, the server default for TellerLoginProtection:MinimumPasscodeLength.
 * The SPA does not read that setting, and elections.form.electionPasscodeMinLength
 * states the same 6.
 */
export const TELLER_PASSCODE_MIN_LENGTH = 6;

/**
 * Empty is allowed. A value equal to the passcode already stored is allowed
 * so an owner can save the rest of the election without lengthening a legacy code.
 * Any other value shorter than {@link TELLER_PASSCODE_MIN_LENGTH} is rejected.
 */
export function tellerPasscodeLengthError(
  value: unknown,
  original?: string | null,
): "too-short" | null {
  if (typeof value !== "string" || value.length === 0) {
    return null;
  }
  if (original && value === original) {
    return null;
  }
  if (value.length < TELLER_PASSCODE_MIN_LENGTH) {
    return "too-short";
  }
  return null;
}
