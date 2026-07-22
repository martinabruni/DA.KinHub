export interface ApiProblem {
  title: string;
  detail: string;
  status: number;
  code?: string;
  traceId?: string;
}

export type KinHubBootstrap =
  | { state: "family"; familyId: string }
  | { state: "onboarding" };

export class ApiError extends Error {}

export class ApiResponseError extends ApiError {
  constructor(public readonly problem: ApiProblem, public readonly correlationId?: string, options?: ErrorOptions) {
    super(problem.detail, options);
  }
}

export class ApiNetworkError extends ApiError {
  constructor(message: string, options?: ErrorOptions) {
    super(message, options);
  }
}

export class KinHubApiClient {
  constructor(private readonly accessToken: () => Promise<string>) {}

  async getKinHubBootstrap(signal?: AbortSignal): Promise<KinHubBootstrap> {
    return this.request<KinHubBootstrap>("/api/kinhub/bootstrap", { signal });
  }

  private async request<T>(path: string, init: RequestInit): Promise<T> {
    if (!navigator.onLine) {
      throw new ApiNetworkError("The browser is offline.");
    }

    const correlationId = crypto.randomUUID();

    let token: string;
    try {
      token = await this.accessToken();
    } catch (error) {
      throw new ApiResponseError(
        { title: "Unauthorized", detail: "A valid KinHub API token is required.", status: 401, code: "auth.required" },
        correlationId,
        { cause: error }
      );
    }

    let response: Response;
    try {
      response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ""}${path}`, {
        ...init,
        cache: "no-store",
        credentials: "omit",
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
          "X-Correlation-ID": correlationId,
          ...init.headers
        }
      });
    } catch (error) {
      throw new ApiNetworkError("The network request failed.", { cause: error });
    }

    if (!response.ok) {
      throw new ApiResponseError(await readProblem(response), response.headers.get("X-Correlation-ID") ?? correlationId);
    }

    return response.json() as Promise<T>;
  }
}

async function readProblem(response: Response): Promise<ApiProblem> {
  try {
    return await response.json() as ApiProblem;
  } catch {
    return {
      title: response.statusText || "Unexpected response",
      detail: "The server returned an unexpected response.",
      status: response.status,
      code: "response.invalid"
    };
  }
}
