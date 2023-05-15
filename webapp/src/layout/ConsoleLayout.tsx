import { useAuthState } from "@app/auth";
import { AppSelector, MobileSidebar, NavMenu, UserMenu } from "@app/navigation";
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
        <div className="flex flex-grow flex-col overflow-y-auto border-r border-default bg-gray-50">
          <div className="p-2 border-b border-default">
            <AppSelector />
          </div>
          <div className="p-2 flex flex-grow flex-col mt-2">
            <NavMenu />
          </div>
          <div className="p-2 border-t border-default">
            <UserMenu user={auth.user} />
          </div>
        </div>
      </div>

      <div className="lg:pl-64">
        <div className="flex flex-col lg:max-w-6xl">
          {/* sidebar for mobile */}
          <div className="lg:hidden sticky top-0 z-30 flex h-12 flex-shrink-0 border-b border-default bg-gray-50">
            <MobileSidebar.Button onClick={() => setSidebarOpen(true)} />
            <div className="flex flex-grow items-center px-2 border-r border-default">
              <AppSelector />
            </div>
            <div className="flex items-center px-2">
              <UserMenu user={auth.user} />
            </div>
          </div>

          <main className="flex-1 p-4 lg:px-8">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
}
