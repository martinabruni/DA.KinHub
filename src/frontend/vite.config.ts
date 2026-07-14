import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";
export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      manifest: {
        name: "KinHub",
        short_name: "KinHub",
        start_url: "/",
        display: "standalone",
        theme_color: "#2563eb",
        icons: [],
      },
      workbox: { navigateFallback: "/index.html" },
    }),
  ],
});
