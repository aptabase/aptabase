import { ThemeProvider } from "@features/theme";
import "@fontsource/inter";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "cal-sans";
import { Provider } from "jotai";
import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import { initAnalytics } from "./analytics";
import { ConfigProvider } from "./config";
import router from "./router";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
    },
  },
});

initAnalytics();

const root = document.getElementById("root") as HTMLElement;
ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <ConfigProvider>
      <ThemeProvider>
          <Provider>
            <QueryClientProvider client={queryClient}>
              <RouterProvider router={router} />
            </QueryClientProvider>
          </Provider>
      </ThemeProvider>
    </ConfigProvider>
  </React.StrictMode>
);
