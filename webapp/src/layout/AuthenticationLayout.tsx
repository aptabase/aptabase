import { Outlet } from "react-router-dom";

export function AuthenticationLayout() {
  return (
    <div className="flex min-h-full flex-col md:justify-center py-12 px-4 lg:px-8 bg-gray-50">
      <Outlet />
    </div>
  );
}
