import { createContext, useContext, type ReactNode } from "react";

const AccountProfileContext = createContext<string | undefined>(undefined);

export function AccountProfileProvider({ accountName, children }: { accountName?: string; children: ReactNode }) {
  return <AccountProfileContext.Provider value={accountName}>{children}</AccountProfileContext.Provider>;
}

export function useAccountProfileName() {
  return useContext(AccountProfileContext);
}
