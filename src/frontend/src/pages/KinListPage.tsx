import { useRef } from "react";
import { KinListAccessGate } from "../components/KinListAccessGate";
import { KinListView } from "../components/KinListView";
import { PageScaffold } from "../components/PageScaffold";
import { useKinHubApiClient } from "../components/KinHubFamilyBootstrap";

export function KinListPage() {
  const titleRef = useRef<HTMLHeadingElement>(null);
  const client = useKinHubApiClient();

  return (
    <PageScaffold routeId="kinlist" titleRef={titleRef}>
      <KinListAccessGate titleRef={titleRef}>{(familyId) => <KinListView familyId={familyId} client={client} />}</KinListAccessGate>
    </PageScaffold>
  );
}
