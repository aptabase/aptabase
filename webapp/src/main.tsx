import "@fontsource/inter";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "cal-sans";
import React from "react";
import ReactDOM from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import { initAnalytics } from "./analytics";
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
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </React.StrictMode>
);
