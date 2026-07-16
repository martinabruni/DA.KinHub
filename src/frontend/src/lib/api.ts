export interface Project {
  id: string;
  name: string;
  stage: string;
  createdAt: string;
}

export interface ApiProblem {
  title: string;
  detail: string;
  status: number;
  code?: string;
  traceId?: string;
}

export class ApiError extends Error {
  constructor(public readonly problem: ApiProblem) {
    super(problem.detail);
  }
}

export class KinHubApiClient {
  constructor(private readonly accessToken: () => Promise<string | null>) {}

  async listProjects(signal?: AbortSignal): Promise<Project[]> {
    return this.request<Project[]>("/api/projects", { signal });
  }

  async createProject(name: string): Promise<Project> {
    return this.request<Project>("/api/projects", { method: "POST", body: JSON.stringify({ name }) });
  }

  private async request<T>(path: string, init: RequestInit): Promise<T> {
    const token = await this.accessToken();
    const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ""}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        "X-Correlation-ID": crypto.randomUUID(),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init.headers
      }
    });
    if (!response.ok) throw new ApiError(await response.json() as ApiProblem);
    return response.json() as Promise<T>;
  }
}
