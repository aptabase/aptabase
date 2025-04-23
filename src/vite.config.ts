import react from "@vitejs/plugin-react-swc";
import path from "path";
import mkcert from "vite-plugin-mkcert";
import { defineConfig } from "vite";
const pkg = require("./package.json");

// http://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
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
    host: '0.0.0.0',
    port: 3000,
    headers: {
      "Content-Security-Policy": `default-src 'self' 'unsafe-inline'; img-src 'self' data: http:; style-src 'self' 'unsafe-inline' http://client.crisp.chat; script-src 'self' 'unsafe-inline' http://client.crisp.chat; font-src 'self' http://client.crisp.chat; connect-src http://raw.githubusercontent.com wss://client.relay.crisp.chat http://client.crisp.chat wss://localhost:3000 http://localhost:3000`,
    },
    proxy: {
      "/uploads": {
        target: "http://localhost:5251",
        changeOrigin: true,
        secure: false,
      },
      "/api": {
        target: "http://localhost:5251",
        changeOrigin: true,
        secure: false,
      },
      "/webhook": {
        target: "http://localhost:5251",
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
