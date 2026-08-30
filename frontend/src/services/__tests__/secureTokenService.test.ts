import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { secureTokenService } from "../secureTokenService";

describe("secureTokenService", () => {
  let cookieJar: string[];

  beforeEach(() => {
    cookieJar = [];
    Object.defineProperty(document, "cookie", {
      configurable: true,
      get: () => cookieJar.join("; "),
      set: (value: string) => {
        cookieJar.push(value);
      },
    });
  });

  afterEach(() => {
    // Restore jsdom's default cookie implementation so this suite does not leak.
    Reflect.deleteProperty(document, "cookie");
    vi.restoreAllMocks();
  });

  it("setCookie always includes secure and samesite=strict", () => {
    secureTokenService.setCookie("user_name", "Ada Lovelace");

    expect(cookieJar).toHaveLength(1);
    const written = cookieJar[0].toLowerCase();
    expect(written).toContain("secure");
    expect(written).toContain("samesite=strict");
    expect(written).toContain("path=/");
    expect(written).toContain("max-age=");
    expect(cookieJar[0]).toContain(
      `user_name=${encodeURIComponent("Ada Lovelace")}`,
    );
  });

  it("setUserInfoCookies writes only provided fields with secure flags", () => {
    secureTokenService.setUserInfoCookies({
      email: "a@example.com",
      name: "Ada",
      authMethod: "Google",
    });

    expect(cookieJar).toHaveLength(3);
    for (const entry of cookieJar) {
      const lower = entry.toLowerCase();
      expect(lower).toContain("; secure");
      expect(lower).toContain("samesite=strict");
    }
    expect(cookieJar.some((c) => c.startsWith("user_email="))).toBe(true);
    expect(cookieJar.some((c) => c.startsWith("user_name="))).toBe(true);
    expect(cookieJar.some((c) => c.startsWith("auth_method="))).toBe(true);
  });

  it("setUserInfoCookies skips empty optional name", () => {
    secureTokenService.setUserInfoCookies({
      email: "a@example.com",
      authMethod: "Local",
    });

    expect(cookieJar).toHaveLength(2);
    expect(cookieJar.some((c) => c.startsWith("user_name="))).toBe(false);
  });

  it("setUserNameCookie clears with max-age=0 and secure flags", () => {
    secureTokenService.setUserNameCookie(null);

    expect(cookieJar).toHaveLength(1);
    const written = cookieJar[0].toLowerCase();
    expect(written).toContain("user_name=");
    expect(written).toContain("max-age=0");
    expect(written).toContain("secure");
    expect(written).toContain("samesite=strict");
  });

  it("setUserEmailCookie sets and clears with secure flags", () => {
    secureTokenService.setUserEmailCookie("b@example.com");
    secureTokenService.setUserEmailCookie(null);

    expect(cookieJar).toHaveLength(2);
    expect(cookieJar[0]).toContain(
      `user_email=${encodeURIComponent("b@example.com")}`,
    );
    expect(cookieJar[1].toLowerCase()).toContain("max-age=0");
    expect(cookieJar[1].toLowerCase()).toContain("secure");
  });

  it("clearAuthData expires all identity cookies with secure flags", () => {
    secureTokenService.clearAuthData();

    expect(cookieJar).toHaveLength(3);
    for (const entry of cookieJar) {
      const lower = entry.toLowerCase();
      expect(lower).toContain("max-age=0");
      expect(lower).toContain("secure");
      expect(lower).toContain("samesite=strict");
    }
  });

  it("isVoterAuthenticated reads the non-httpOnly voter_session flag", () => {
    cookieJar.push("voter_session=1");
    expect(secureTokenService.isVoterAuthenticated()).toBe(true);
  });

  it("clearVoterSession expires the voter_session flag with secure flags", () => {
    secureTokenService.clearVoterSession();

    expect(cookieJar).toHaveLength(1);
    const written = cookieJar[0].toLowerCase();
    expect(written).toContain("voter_session=");
    expect(written).toContain("max-age=0");
    expect(written).toContain("secure");
    expect(written).toContain("samesite=strict");
  });

  it("getCookie decodes URI-encoded values", () => {
    // Simulate a browser cookie store string (name=value pairs only; no attributes).
    // Use the shared mock jar so we do not reassign document.cookie without Secure
    // (which triggers CodeQL js/clear-text-cookie on sensitive names).
    cookieJar.length = 0;
    cookieJar.push(`user_name=${encodeURIComponent("Ada Lovelace")}`);

    expect(secureTokenService.getCookie("user_name")).toBe("Ada Lovelace");
  });
});
