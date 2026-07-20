import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

interface ConnectivityState {
  online: boolean;
}

const ConnectivityContext = createContext<ConnectivityState>({ online: navigator.onLine });

export function ConnectivityProvider({ children }: { children: ReactNode }) {
  const [online, setOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const handleOnline = () => setOnline(true);
    const handleOffline = () => setOnline(false);
    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);
    return () => {
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, []);

  return <ConnectivityContext.Provider value={{ online }}>{children}</ConnectivityContext.Provider>;
}

export function useConnectivity() {
  return useContext(ConnectivityContext);
}
