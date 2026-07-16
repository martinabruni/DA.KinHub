import { Component, type ErrorInfo, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "./PageScaffold";

class Boundary extends Component<{ children: ReactNode; fallback: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  componentDidCatch(error: Error, info: ErrorInfo) { console.error("KinHub UI boundary", error, info.componentStack); }
  render() { return this.state.failed ? this.props.fallback : this.props.children; }
}

export function AppErrorBoundary({ children }: { children: ReactNode }) {
  const { t } = useTranslation(["pages", "common"]);
  const fallback = (
    <PageScaffold routeId="error">
      <p>{t("error.description", { ns: "pages" })}</p>
      <div className="actions"><button className="button" type="button" onClick={() => window.location.reload()}>{t("actions.retry", { ns: "common" })}</button><Link className="button secondary" to="/">{t("notFound.home", { ns: "pages" })}</Link></div>
    </PageScaffold>
  );
  return <Boundary fallback={fallback}>{children}</Boundary>;
}
