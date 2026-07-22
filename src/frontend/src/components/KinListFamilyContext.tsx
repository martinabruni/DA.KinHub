import { createContext, useContext, useState, type Dispatch, type ReactNode, type SetStateAction } from "react";

interface KinListFamilyContextValue {
  familyId: string | null;
  setFamilyId: Dispatch<SetStateAction<string | null>>;
}

const KinListFamilyContext = createContext<KinListFamilyContextValue | null>(null);

export function KinListFamilyProvider({ children }: { children: ReactNode }) {
  const [familyId, setFamilyId] = useState<string | null>(null);

  return <KinListFamilyContext.Provider value={{ familyId, setFamilyId }}>{children}</KinListFamilyContext.Provider>;
}

export function useKinListFamily() {
  const context = useContext(KinListFamilyContext);
  if (context === null) {
    throw new Error("KinList family context is not available.");
  }

  return context;
}
