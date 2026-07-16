import { useMsal } from "@azure/msal-react";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "../components/PageScaffold";
import { authConfig } from "../lib/auth";
import { KinHubApiClient, type Project } from "../lib/api";

export function ProjectsPage() {
  const { t } = useTranslation(["pages", "common"]);
  const { instance, accounts } = useMsal();
  const [projects, setProjects] = useState<Project[]>([]);
  const [name, setName] = useState("");
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [createError, setCreateError] = useState(false);
  const client = useMemo(() => new KinHubApiClient(async () => {
    if (!authConfig.configured || !accounts[0]) return null;
    return (await instance.acquireTokenSilent({ account: accounts[0], scopes: [authConfig.apiScope] })).accessToken;
  }), [accounts, instance]);

  const load = async (signal?: AbortSignal) => {
    setState("loading");
    try { setProjects(await client.listProjects(signal)); setState("ready"); }
    catch (error) { if (!(error instanceof DOMException && error.name === "AbortError")) setState("error"); }
  };
  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal);
    return () => controller.abort();
  }, [client]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setCreateError(false);
    try {
      const project = await client.createProject(name);
      setProjects((current) => [...current, project]);
      setName("");
    } catch { setCreateError(true); }
  };

  return (
    <PageScaffold routeId="projects">
      <p className="lead">{t("projects.intro", { ns: "pages" })}</p>
      <form className="inline-form" onSubmit={(event) => { void submit(event); }}>
        <label><span>{t("projects.nameLabel", { ns: "pages" })}</span><input required minLength={3} maxLength={120} value={name} placeholder={t("projects.namePlaceholder", { ns: "pages" })} onChange={(event) => setName(event.target.value)} /></label>
        <button className="button" type="submit">{t("actions.create", { ns: "common" })}</button>
      </form>
      {createError && <p className="error-message" role="alert">{t("projects.createError", { ns: "pages" })}</p>}
      {state === "loading" && <div className="state-card" aria-live="polite">{t("states.loading", { ns: "common" })}</div>}
      {state === "error" && <div className="state-card"><p>{t("projects.loadError", { ns: "pages" })}</p><button type="button" className="button secondary" onClick={() => { void load(); }}>{t("actions.retry", { ns: "common" })}</button></div>}
      {state === "ready" && projects.length === 0 && <div className="state-card">{t("projects.empty", { ns: "pages" })}</div>}
      {state === "ready" && projects.length > 0 && <ul className="project-list">{projects.map((project) => <li key={project.id}><strong>{project.name}</strong><span>{t("projects.stage", { ns: "pages", stage: project.stage })}</span></li>)}</ul>}
    </PageScaffold>
  );
}
