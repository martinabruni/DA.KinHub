import { KinListAccessGate } from "../components/KinListAccessGate";
import { PageScaffold } from "../components/PageScaffold";

export function KinListPage() {
  return (
    <PageScaffold routeId="kinlist">
      <KinListAccessGate />
    </PageScaffold>
  );
}
