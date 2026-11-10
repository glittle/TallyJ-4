/** Minimum length for a new local password. Identity uses the same number. */
export const localPasswordMinimumLength = 8;

export function localPasswordProblems(
  value: string,
  text: (key: string) => string,
): string[] {
  const errors: string[] = [];
  if (value.length < localPasswordMinimumLength) {
    errors.push(text("auth.passwordMinLength8"));
  }
  if (!/(?=.*[a-z])/.test(value)) {
    errors.push(text("auth.passwordRequireLowercase"));
  }
  if (!/(?=.*[A-Z])/.test(value)) {
    errors.push(text("auth.passwordRequireUppercase"));
  }
  if (!/(?=.*\d)/.test(value)) {
    errors.push(text("auth.passwordRequireDigit"));
  }
  if (!/(?=.*[^a-zA-Z\d])/.test(value)) {
    errors.push(text("auth.passwordRequireSpecial"));
  }
  return errors;
}
