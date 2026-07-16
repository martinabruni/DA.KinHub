import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

const version = readFileSync(resolve(import.meta.dirname, "../../VERSION"), "utf8").trim();

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "prompt",
      injectRegister: false,
      includeAssets: ["icon.svg", "staticwebapp.config.json"],
      manifest: {
        name: "KinHub",
        short_name: "KinHub",
        description: "Family services in one calm, accessible place",
        lang: "it",
        start_url: "/",
        display: "standalone",
        background_color: "#f7f6f2",
        theme_color: "#35594c",
        icons: [
          { src: "/icon.svg", sizes: "any", type: "image/svg+xml", purpose: "any maskable" }
        ]
      },
      workbox: {
        navigateFallback: "/index.html",
        navigateFallbackDenylist: [/^\/api\//, /^\/health\//],
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname === "/release-notes.json",
            handler: "NetworkFirst",
            options: { cacheName: "kinhub-version", networkTimeoutSeconds: 3 }
          }
        ]
      }
    })
  ],
  define: {
    __APP_VERSION__: JSON.stringify(version),
    __COMMIT_SHA__: JSON.stringify(process.env.GITHUB_SHA?.slice(0, 12) ?? process.env.COMMIT_SHA ?? "local"),
    __BUILD_DATE__: JSON.stringify(process.env.BUILD_DATE ?? new Date().toISOString()),
    __BUILD_ENVIRONMENT__: JSON.stringify(process.env.BUILD_ENVIRONMENT ?? "Development")
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          react: ["react", "react-dom", "react-router-dom"],
          auth: ["@azure/msal-browser", "@azure/msal-react"],
          content: ["i18next", "react-i18next", "react-markdown"]
        }
      }
    }
  },
  server: {
    port: 5173,
    proxy: { "/api": "http://localhost:7071", "/health": "http://localhost:7071" }
  }
});
