import { MsalProvider } from "@azure/msal-react";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { App } from "./App";
import { ConnectivityProvider } from "./components/ConnectivityProvider";
import { KinListFamilyProvider } from "./components/KinListFamilyContext";
import { ThemeProvider } from "./components/ThemeProvider";
import "./i18n";
import { msalInstance } from "./lib/auth";
import "./styles.css";

await msalInstance.initialize();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <MsalProvider instance={msalInstance}>
      <ThemeProvider>
        <ConnectivityProvider>
          <KinListFamilyProvider>
            <BrowserRouter><App /></BrowserRouter>
          </KinListFamilyProvider>
        </ConnectivityProvider>
      </ThemeProvider>
    </MsalProvider>
  </StrictMode>
);
