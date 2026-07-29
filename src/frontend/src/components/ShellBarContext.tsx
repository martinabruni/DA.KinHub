import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

interface ShellBarContextValue {
  contextualBar: ReactNode | null;
  setContextualBar: (bar: ReactNode | null) => void;
}

const ShellBarContext = createContext<ShellBarContextValue | null>(null);

export function ShellBarProvider({ children }: { children: ReactNode }) {
  const [contextualBar, setContextualBar] = useState<ReactNode | null>(null);
  const value = useMemo(() => ({ contextualBar, setContextualBar }), [contextualBar]);
  return <ShellBarContext.Provider value={value}>{children}</ShellBarContext.Provider>;
}

export function useShellBar() {
  const value = useContext(ShellBarContext);
  if (!value) {
    throw new Error("useShellBar must be used inside ShellBarProvider");
  }

  return value;
}
