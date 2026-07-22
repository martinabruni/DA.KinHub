import { createContext, useContext, useState, type Dispatch, type ReactNode, type SetStateAction } from "react";

interface KinHubFamilyContextValue {
  familyId: string | null;
  setFamilyId: Dispatch<SetStateAction<string | null>>;
}

const KinHubFamilyContext = createContext<KinHubFamilyContextValue | null>(null);

export function KinHubFamilyProvider({ children }: { children: ReactNode }) {
  const [familyId, setFamilyId] = useState<string | null>(null);

  return <KinHubFamilyContext.Provider value={{ familyId, setFamilyId }}>{children}</KinHubFamilyContext.Provider>;
}

export function useKinHubFamily() {
  const context = useContext(KinHubFamilyContext);
  if (context === null) {
    throw new Error("KinHub family context is not available.");
  }

  return context;
}
