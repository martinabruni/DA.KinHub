import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo,
  type Configuration,
  type IPublicClientApplication
} from "@azure/msal-browser";

const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID ?? "";
const clientId = import.meta.env.VITE_ENTRA_FRONTEND_CLIENT_ID ?? "";
const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE ?? "";
const authority = import.meta.env.VITE_ENTRA_AUTHORITY ?? "";
const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI ?? window.location.origin;
const configured = [tenantId, clientId, apiScope, authority].every((value) => value && !value.startsWith("<"));

const configuration: Configuration = {
  auth: {
    clientId: configured ? clientId : "00000000-0000-0000-0000-000000000000",
    authority: configured ? authority : "https://login.microsoftonline.com/common",
    redirectUri,
    postLogoutRedirectUri: window.location.origin
  },
  cache: { cacheLocation: "memoryStorage" },
  system: { allowPlatformBroker: false }
};

export const authConfig = { configured, apiScope, redirectUri };
export const msalInstance = new PublicClientApplication(configuration);

export function getActiveAccount(instance: IPublicClientApplication) {
  return instance.getActiveAccount() ?? instance.getAllAccounts()[0] ?? null;
}

export async function loginForApiAccess(instance: IPublicClientApplication) {
  const result = await instance.loginPopup({ scopes: [authConfig.apiScope], prompt: "select_account" });
  instance.setActiveAccount(result.account);
  return result.account;
}

export async function logoutCurrentAccount(instance: IPublicClientApplication) {
  await instance.logoutPopup({ account: getActiveAccount(instance) ?? undefined });
}

export async function acquireApiAccessToken(instance: IPublicClientApplication, account: AccountInfo | null) {
  if (!authConfig.configured || !account) {
    throw new InteractionRequiredAuthError("account_required", "No active account is available.");
  }

  instance.setActiveAccount(account);
  return (await instance.acquireTokenSilent({ account, scopes: [authConfig.apiScope] })).accessToken;
}

export function isInteractionRequiredError(error: unknown) {
  return error instanceof InteractionRequiredAuthError;
}
