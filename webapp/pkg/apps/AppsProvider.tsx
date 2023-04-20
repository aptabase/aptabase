import { ErrorState, LoadingState } from "@app/primitives";
import { useQuery } from "@tanstack/react-query";
import { createContext, useContext } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { toast } from "sonner";
import { Application, createApp, deleteApp, listApps, updateApp } from "./apps";

type Props = {
  children: React.ReactNode;
};

type AppsContextType = {
  apps: Application[];
  createApp: (name: string) => Promise<Application>;
  deleteApp: (appId: string) => Promise<void>;
  updateApp: (appId: string, name: string) => Promise<Application>;
};

const AppsContext = createContext<AppsContextType>({ apps: [], createApp, deleteApp, updateApp });

export function AppsProvider(props: Props) {
  const location = useLocation();
  const { isLoading, isError, data, refetch } = useQuery(["apps"], listApps);

  const createAppAndRefresh = async (name: string): Promise<Application> => {
    const app = await createApp(name);
    toast(`${name} app was created. 🎉`);
    await refetch();
    return app;
  };

  const deleteAppAndRefresh = async (appId: string): Promise<void> => {
    await deleteApp(appId);
    await refetch();
  };

  const updateAppAndRefresh = async (appId: string, name: string): Promise<Application> => {
    const app = await updateApp(appId, name);
    await refetch();
    return app;
  };

  if (isLoading) return <LoadingState size="lg" color="primary" delay={0} />;
  if (isError) return <ErrorState />;

  if (data?.length === 0 && location.pathname !== "/welcome") {
    return <Navigate to="/welcome" />;
  }

  return (
    <AppsContext.Provider
      value={{ apps: data ?? [], createApp: createAppAndRefresh, deleteApp: deleteAppAndRefresh, updateApp: updateAppAndRefresh }}
    >
      {isLoading ? null : props.children}
    </AppsContext.Provider>
  );
}

export function useApps(): AppsContextType {
  return useContext(AppsContext);
}
