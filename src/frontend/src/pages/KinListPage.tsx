import { useRef } from "react";
import { KinListAccessGate } from "../components/KinListAccessGate";
import { PageScaffold } from "../components/PageScaffold";

export function KinListPage() {
  const titleRef = useRef<HTMLHeadingElement>(null);

  return (
    <PageScaffold routeId="kinlist" titleRef={titleRef}>
      <KinListAccessGate titleRef={titleRef} />
    </PageScaffold>
  );
}
