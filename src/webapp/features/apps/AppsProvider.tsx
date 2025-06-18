import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { useLocalStorage } from "@hooks/use-localstorage";
import { useQuery } from "@tanstack/react-query";
import { createContext, useContext } from "react";
import { Navigate, useLocation, useParams } from "react-router-dom";
import { toast } from "sonner";
import { Application, createApp, deleteApp, listApps, updateApp } from "./apps";
import { OwnershipTransferStartupCheck } from "./OwnershipTransferStartupCheck";

type Props = {
  children: React.ReactNode;
};

export type BuildMode = "debug" | "release";

type AppsContextType = {
  apps: Application[];
  buildMode: BuildMode;
  createApp: (name: string) => Promise<Application>;
  deleteApp: (appId: string) => Promise<void>;
  updateApp: (appId: string, name: string, icon: string) => Promise<Application>;
  switchBuildMode: (mode: BuildMode) => void;
};

const AppsContext = createContext<AppsContextType | undefined>(undefined);

export function AppsProvider(props: Props) {
  const location = useLocation();
  const [buildMode, setBuildMode] = useLocalStorage<BuildMode>("buildmode", "debug");

  const { isLoading, isError, data, refetch } = useQuery({ queryKey: ["apps"], queryFn: listApps });

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

  const updateAppAndRefresh = async (appId: string, name: string, icon: string): Promise<Application> => {
    const app = await updateApp(appId, name, icon);
    toast(`${name} app was successfully updated.`);
    await refetch();
    return app;
  };

  if (isLoading) return <LoadingState size="lg" color="primary" delay={0} />;
  if (isError) return <ErrorState />;

  if (data?.length === 0 && location.pathname !== "/") {
    return <Navigate to="/" />;
  }

  return (
    <AppsContext.Provider
      value={{
        apps: data ?? [],
        buildMode,
        switchBuildMode: setBuildMode,
        createApp: createAppAndRefresh,
        deleteApp: deleteAppAndRefresh,
        updateApp: updateAppAndRefresh,
      }}
    >
      {props.children}
      <OwnershipTransferStartupCheck />
    </AppsContext.Provider>
  );
}

export function useApps(): AppsContextType {
  const ctx = useContext(AppsContext);
  if (!ctx) {
    throw new Error("useApps must be used within a AppsProvider");
  }

  return ctx;
}

export function useCurrentApp(): Application | undefined {
  const { apps } = useApps();
  let { id } = useParams();

  return apps.find((app) => app.id === id);
}
