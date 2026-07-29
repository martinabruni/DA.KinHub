import { describe, expect, it } from "vitest";
import { authConfig } from "./auth";

describe("MSAL cache configuration", () => {
  it("uses sessionStorage and not durable or process-only storage", () => {
    expect(authConfig.cacheLocation).toBe("sessionStorage");
    expect(authConfig.cacheLocation).not.toBe("memoryStorage");
    expect(authConfig.cacheLocation).not.toBe("localStorage");
  });
});
