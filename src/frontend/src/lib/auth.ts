import { PublicClientApplication, type Configuration } from "@azure/msal-browser";

const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID ?? "";
const clientId = import.meta.env.VITE_ENTRA_FRONTEND_CLIENT_ID ?? "";
const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE ?? "";
const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI ?? window.location.origin;
const configured = [tenantId, clientId, apiScope].every((value) => value && !value.startsWith("<"));

const configuration: Configuration = {
  auth: {
    clientId: configured ? clientId : "00000000-0000-0000-0000-000000000000",
    authority: configured ? `https://login.microsoftonline.com/${tenantId}` : "https://login.microsoftonline.com/common",
    redirectUri,
    postLogoutRedirectUri: window.location.origin
  },
  cache: { cacheLocation: "localStorage" },
  system: { allowPlatformBroker: false }
};

export const authConfig = { configured, apiScope, redirectUri };
export const msalInstance = new PublicClientApplication(configuration);
