import { AppsProvider } from "@app/apps";
import { AuthProvider } from "@app/auth";
import { createBrowserRouter } from "react-router-dom";
import { Toaster } from "sonner";
import { AuthenticationLayout, ConsoleLayout } from "./layout";

const router = createBrowserRouter([
  {
    path: "/auth",
    element: <AuthenticationLayout />,
    children: [
      {
        index: true,
        lazy: () => import("./routes/auth/Login"),
      },
      {
        path: "register",
        lazy: () => import("./routes/auth/Register"),
      },
    ],
  },
  {
    path: "/",
    element: (
      <>
        <AuthProvider>
          <AppsProvider>
            <ConsoleLayout />
          </AppsProvider>
        </AuthProvider>
        <Toaster className="hidden lg:block" position="top-right" />
        <Toaster className="block lg:hidden" position="bottom-center" />
      </>
    ),
    children: [
      {
        index: true,
        lazy: () => import("./routes/home/Home"),
      },
      {
        path: "/:id",
        lazy: () => import("./routes/app/Dashboard"),
      },
      {
        path: "/:id/instructions",
        lazy: () => import("./routes/app/Instructions"),
      },
      {
        path: "/:id/settings",
        lazy: () => import("./routes/app/Settings"),
      },
      {
        path: "/billing",
        lazy: () => import("../pkg/billing/BillingPage"),
      },
    ],
  },
  {},
]);

export default router;
