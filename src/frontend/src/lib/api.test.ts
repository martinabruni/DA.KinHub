import { beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { ApiNetworkError, type ApiResponseError, KinHubApiClient } from "./api";

describe("KinHubApiClient", () => {
  let fetchMock: Mock;

  beforeEach(() => {
    vi.restoreAllMocks();
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: true });
    fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    vi.stubGlobal("crypto", { randomUUID: () => "test-correlation-id" });
  });

  it("sends authenticated bootstrap requests with no-store and correlation ID", async () => {
    fetchMock.mockResolvedValue(new Response(JSON.stringify({ state: "onboarding" }), { status: 200 }));
    const client = new KinHubApiClient(() => Promise.resolve("token-123"));

    await expect(client.getKinListBootstrap()).resolves.toEqual({ state: "onboarding" });
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [path, requestInit] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(path).toBe("/api/kinlist/bootstrap");
    expect(requestInit.cache).toBe("no-store");
    expect(requestInit.credentials).toBe("omit");
    expect(requestInit.headers).toEqual(expect.objectContaining({
      Accept: "application/json",
      Authorization: "Bearer token-123",
      "X-Correlation-ID": "test-correlation-id"
    }));
  });

  it("fails as network error when the browser is offline", async () => {
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: false });
    const client = new KinHubApiClient(() => Promise.resolve("token-123"));

    await expect(client.getKinListBootstrap()).rejects.toBeInstanceOf(ApiNetworkError);
  });

  it("maps problem details responses", async () => {
    fetchMock.mockResolvedValue(new Response(JSON.stringify({ title: "Forbidden", detail: "Denied", status: 403, code: "family.accessDenied" }), {
      status: 403,
      headers: { "X-Correlation-ID": "server-correlation-id" }
    }));
    const client = new KinHubApiClient(() => Promise.resolve("token-123"));

    try {
      await client.getKinListBootstrap();
      throw new Error("The response should have failed.");
    } catch (error) {
      const apiError = error as ApiResponseError;
      expect(apiError.problem.status).toBe(403);
      expect(apiError.problem.code).toBe("family.accessDenied");
      expect(apiError.correlationId).toBe("server-correlation-id");
    }
  });
});
