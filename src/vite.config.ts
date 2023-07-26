import react from "@vitejs/plugin-react-swc";
import path from "path";
import { defineConfig } from "vite";
const pkg = require("./package.json");

// https://vitejs.dev/config/
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
          motion: ["framer-motion"],
          highlight: ["rehype-highlight", "remark-gfm"],
        },
      },
    },
  },
  server: {
    port: 3000,
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
        find: "@features/env",
        replacement: path.resolve(__dirname, "./webapp/features/env"),
      },
      {
        find: "@features/billing",
        replacement: path.resolve(__dirname, "./webapp/features/billing"),
      },
      {
        find: "@features/support",
        replacement: path.resolve(__dirname, "./webapp/features/support"),
      },
      {
        find: "@features/primitives",
        replacement: path.resolve(__dirname, "./webapp/features/primitives"),
      },
      {
        find: "@features/theme",
        replacement: path.resolve(__dirname, "./webapp/features/theme"),
      },
      {
        find: "@features/navigation",
        replacement: path.resolve(__dirname, "./webapp/features/navigation"),
      },
      {
        find: "@features/apps",
        replacement: path.resolve(__dirname, "./webapp/features/apps"),
      },
      {
        find: "@features/auth",
        replacement: path.resolve(__dirname, "./webapp/features/auth"),
      },
      {
        find: "@features/widgets",
        replacement: path.resolve(__dirname, "./webapp/features/widgets"),
      },
    ],
  },
});
