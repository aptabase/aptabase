import { AppsProvider } from "@app/apps";
import { AuthProvider } from "@app/auth";
import { NavigationProvider } from "@app/navigation";
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
            <NavigationProvider>
              <ConsoleLayout />
            </NavigationProvider>
          </AppsProvider>
        </AuthProvider>
        <Toaster className="hidden lg:block" position="top-right" />
        <Toaster className="block lg:hidden" position="bottom-center" />
      </>
    ),
    children: [
      {
        index: true,
        lazy: () => import("./routes/app/Dashboard"),
      },
      {
        path: "/welcome",
        lazy: () => import("./routes/Welcome"),
      },
      {
        path: "/instructions",
        lazy: () => import("./routes/app/Instructions"),
      },
      {
        path: "/settings",
        lazy: () => import("./routes/app/Settings"),
      },
    ],
  },
  {},
]);

export default router;
