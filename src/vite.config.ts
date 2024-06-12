import react from "@vitejs/plugin-react-swc";
import path from "path";
import mkcert from "vite-plugin-mkcert";
import { defineConfig } from "vite";
const pkg = require("./package.json");

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), mkcert()],
  define: {
    "import.meta.env.APP_VERSION": JSON.stringify(pkg.version),
  },
  publicDir: "webapp/public",
  build: {
    outDir: "wwwroot",
    rollupOptions: {
      output: {
        manualChunks: {
          react: ["react", "react-router-dom", "react-dom"],
          chartjs: ["chart.js", "chartjs-plugin-annotation"],
        },
      },
    },
  },
  server: {
    port: 3000,
    https: true,
    headers: {
      "Content-Security-Policy": `default-src 'self' 'unsafe-inline'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline' https://client.crisp.chat; script-src 'self' 'unsafe-inline' https://client.crisp.chat; font-src 'self' https://client.crisp.chat; connect-src https://raw.githubusercontent.com wss://client.relay.crisp.chat https://client.crisp.chat wss://localhost:3000 https://localhost:3000`,
    },
    proxy: {
      "/uploads": {
        target: "https://localhost:5251",
        changeOrigin: true,
        secure: false,
      },
      "/api": {
        target: "https://localhost:5251",
        changeOrigin: true,
        secure: false,
      },
      "/webhook": {
        target: "https://localhost:5251",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  resolve: {
    conditions: ["development", "browser"],
    alias: [
      {
        find: "@components",
        replacement: path.resolve(__dirname, "./webapp/components"),
      },
      {
        find: "@fns",
        replacement: path.resolve(__dirname, "./webapp/fns"),
      },
      {
        find: "@hooks",
        replacement: path.resolve(__dirname, "./webapp/hooks"),
      },
      {
        find: "@features",
        replacement: path.resolve(__dirname, "./webapp/features"),
      },
    ],
  },
});
