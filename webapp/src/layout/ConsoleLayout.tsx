import { AppSelector } from "@app/apps";
import { useAuthState } from "@app/auth";
import { CurrentUsage } from "@app/billing";
import { isBillingEnabled } from "@app/env";
import { MobileSidebar, NavMenu, UserMenu } from "@app/navigation";
import { useState } from "react";
import { Navigate, Outlet } from "react-router-dom";

export function ConsoleLayout() {
  const auth = useAuthState();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  if (!auth.user) {
    return <Navigate to="/auth" replace={true} />;
  }

  return (
    <div>
      <MobileSidebar open={sidebarOpen} onClose={setSidebarOpen} />

      {/* Static sidebar for desktop */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:flex lg:w-64 lg:flex-col">
        <div className="flex flex-grow flex-col overflow-y-auto border-r">
          <div className="flex justify-between items-center p-2 border-b">
            <AppSelector />
          </div>
          <div className="p-2 flex flex-grow flex-col mt-2">
            <NavMenu />
          </div>
          {isBillingEnabled && (
            <div className="p-2 border-t">
              <CurrentUsage />
            </div>
          )}
          <div className="p-2 border-t">
            <UserMenu user={auth.user} />
          </div>
        </div>
      </div>

      <div className="lg:pl-64">
        <div className="flex flex-col">
          {/* sidebar for mobile */}
          <div className="lg:hidden sticky top-0 z-30 flex h-12 flex-shrink-0 border-b">
            <MobileSidebar.Button onClick={() => setSidebarOpen(true)} />
            <div className="flex justify-between flex-grow items-center px-2 border-r">
              <AppSelector />
            </div>
            <div className="flex items-center px-2">
              <UserMenu user={auth.user} />
            </div>
          </div>

          <main className="flex-1 p-4 lg:px-8 mx-auto w-full max-w-6xl">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
}
