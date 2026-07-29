import { Component, type ErrorInfo, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { PageScaffold } from "./PageScaffold";
import { Button, ButtonLink } from "./ui/core";
import { StatePanel } from "./ui/feedback";

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
      <StatePanel title={t("error.title", { ns: "pages" })} description={t("error.description", { ns: "pages" })} tone="danger" role="alert" live="assertive" action={<div className="actions"><Button type="button" onClick={() => window.location.reload()}>{t("actions.retry", { ns: "common" })}</Button><ButtonLink variant="secondary" to="/">{t("notFound.home", { ns: "pages" })}</ButtonLink></div>} />
    </PageScaffold>
  );
  return <Boundary fallback={fallback}>{children}</Boundary>;
}
