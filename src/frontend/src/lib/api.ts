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

export interface CreateFamilyRequest {
  name: string;
}

export interface KinHubFamilyState {
  state: "family";
  familyId: string;
}

export interface KinHubServiceCatalogItem {
  key: string;
  route: string;
  name: string;
  description: string;
}

export interface KinHubServiceCatalogResult {
  services: KinHubServiceCatalogItem[];
}

export interface KinListItemCategory {
  id: string;
  name: string;
}

export interface KinListItemAuthor {
  displayName: string | null;
}

export interface KinListItem {
  id: string;
  name: string;
  categories: KinListItemCategory[];
  remainingCategoryCount: number;
  author: KinListItemAuthor;
  version: string;
}

export interface KinListItemsPage {
  items: KinListItem[];
  effectivePageSize: number;
  maxPageSize: number;
  previousCursor: string | null;
  nextCursor: string | null;
}

export interface FamilyDetails {
  name: string;
}

export interface FamilyMember {
  displayName: string | null;
  initials: string | null;
  isCurrentUser: boolean;
}

export interface FamilyMembersPage {
  items: FamilyMember[];
  effectivePageSize: number;
  maxPageSize: number;
  previousCursor: string | null;
  nextCursor: string | null;
}

export interface FamilyInvitationCreator {
  displayName: string | null;
  initials: string | null;
}

export interface FamilyInvitation {
  id: string;
  creator: FamilyInvitationCreator;
  createdAt: string;
  expiresAt: string;
  status: "active";
}

export interface FamilyInvitationsPage {
  items: FamilyInvitation[];
  effectivePageSize: number;
  maxPageSize: number;
  previousCursor: string | null;
  nextCursor: string | null;
}

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

  async createFamily(body: CreateFamilyRequest, signal?: AbortSignal): Promise<KinHubFamilyState> {
    return this.request<KinHubFamilyState>("/api/kinhub/families", {
      method: "POST",
      body: JSON.stringify(body),
      signal,
      headers: {
        "Content-Type": "application/json"
      }
    });
  }

  async getFamilyServices(familyId: string, language: string, signal?: AbortSignal): Promise<KinHubServiceCatalogResult> {
    return this.request<KinHubServiceCatalogResult>(`/api/kinhub/services?${new URLSearchParams({ familyId, language }).toString()}`, { signal });
  }

  async checkServiceAccess(serviceKey: string, familyId: string, signal?: AbortSignal): Promise<void> {
    await this.request<void>(`/api/kinhub/services/${encodeURIComponent(serviceKey)}/access?${new URLSearchParams({ familyId }).toString()}`, { signal, expectNoContent: true });
  }

  async getKinListItems(familyId: string, pageSize: number, cursor?: string | null, signal?: AbortSignal): Promise<KinListItemsPage> {
    const params = new URLSearchParams({ familyId, pageSize: String(pageSize) });
    if (cursor) {
      params.set("cursor", cursor);
    }

    return this.request<KinListItemsPage>(`/api/kinlist/items?${params.toString()}`, { signal });
  }

  async getFamilyDetails(familyId: string, signal?: AbortSignal): Promise<FamilyDetails> {
    return this.request<FamilyDetails>(`/api/kinhub/families/details?${new URLSearchParams({ familyId }).toString()}`, { signal });
  }

  async getFamilyMembers(familyId: string, pageSize: number, cursor?: string | null, signal?: AbortSignal): Promise<FamilyMembersPage> {
    const params = new URLSearchParams({ familyId, pageSize: String(pageSize) });
    if (cursor) {
      params.set("cursor", cursor);
    }

    return this.request<FamilyMembersPage>(`/api/kinhub/families/members?${params.toString()}`, { signal });
  }

  async getFamilyInvitations(familyId: string, pageSize: number, cursor?: string | null, signal?: AbortSignal): Promise<FamilyInvitationsPage> {
    const params = new URLSearchParams({ familyId, pageSize: String(pageSize) });
    if (cursor) {
      params.set("cursor", cursor);
    }

    return this.request<FamilyInvitationsPage>(`/api/kinhub/families/invitations?${params.toString()}`, { signal });
  }

  private async request<T>(path: string, init: RequestInit & { expectNoContent?: boolean }): Promise<T> {
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
      const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");
      const requestUrl = apiBaseUrl && (path === apiBaseUrl || path.startsWith(`${apiBaseUrl}/`))
        ? path
        : `${apiBaseUrl}${path}`;
      response = await fetch(requestUrl, {
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

    if (init.expectNoContent || response.status === 204) {
      return undefined as T;
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
