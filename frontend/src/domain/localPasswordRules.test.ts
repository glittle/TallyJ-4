import { describe, expect, it } from "vitest";
import {
  localPasswordMinimumLength,
  localPasswordProblems,
} from "./localPasswordRules";

const text = (key: string) =>
  ({
    "auth.passwordMinLength8": "Password must be at least 8 characters",
    "auth.passwordRequireLowercase":
      "Password must contain at least one lowercase letter",
    "auth.passwordRequireUppercase":
      "Password must contain at least one uppercase letter",
    "auth.passwordRequireDigit": "Password must contain at least one number",
    "auth.passwordRequireSpecial":
      "Password must contain at least one special character",
  })[key] ?? key;

/** One of each required class, then filler, cut to length. Built here so the spec does not embed a finished sample. */
function sampleOfLength(length: number): string {
  const classes = ["A", "b", "1", "!"];
  return (classes.join("") + "x".repeat(length)).slice(0, length);
}

describe("localPasswordProblems", () => {
  it("accepts 8 characters when the other rules are met", () => {
    const sample = sampleOfLength(localPasswordMinimumLength);
    expect(sample).toHaveLength(8);
    expect(localPasswordProblems(sample, text)).toEqual([]);
  });

  it("rejects 7 characters that otherwise meet the rules", () => {
    const sample = sampleOfLength(localPasswordMinimumLength - 1);
    expect(sample).toHaveLength(7);
    expect(localPasswordProblems(sample, text)).toEqual([
      "Password must be at least 8 characters",
    ]);
  });
});
