import react from "@vitejs/plugin-react-swc";
import path from "path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    include: ["webapp/**/*.test.{ts,tsx}"],
  },
  resolve: {
    alias: [
      { find: "@components", replacement: path.resolve(__dirname, "./webapp/components") },
      { find: "@fns", replacement: path.resolve(__dirname, "./webapp/fns") },
      { find: "@hooks", replacement: path.resolve(__dirname, "./webapp/hooks") },
      { find: "@features", replacement: path.resolve(__dirname, "./webapp/features") },
    ],
  },
});
