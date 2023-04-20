import { Application, useApps } from "@app/apps";
import { createContext, useContext, useEffect, useState } from "react";

type Props = {
  children: React.ReactNode;
};

type NavigationContextType = {
  currentApp: Application | undefined;
  switchApp: (app: Application) => void;
};

const APP_ID_STORAGE_KEY = "aptabase:app-id";

const NavigationContext = createContext<NavigationContextType>({ currentApp: undefined, switchApp: () => {} });

const findApp = (apps: Application[], appId?: string | null) => apps.find((app) => app.id === appId);

export function NavigationProvider(props: Props) {
  const { apps } = useApps();

  const initialApp = findApp(apps, localStorage.getItem(APP_ID_STORAGE_KEY)) || apps[0];
  const [currentApp, setCurrentApp] = useState<Application | undefined>(initialApp);

  // If the app list changes, check if the current app is still in the list
  // If not, switch to the first app in the list
  useEffect(() => {
    if (apps.length === 0) return;

    const app = findApp(apps, currentApp?.id);
    if (!app) {
      switchApp(apps[0]);
    }
  }, [apps]);

  const switchApp = (app: Application) => {
    localStorage.setItem(APP_ID_STORAGE_KEY, app.id);
    setCurrentApp(app);
  };

  return <NavigationContext.Provider value={{ currentApp, switchApp }}>{props.children}</NavigationContext.Provider>;
}

export function useNavigationContext(): NavigationContextType {
  return useContext(NavigationContext);
}

export function useCurrentApp(): Application {
  const ctx = useNavigationContext();

  if (!ctx.currentApp) {
    throw new Error("No current app selected, this should never happen!");
  }

  return ctx.currentApp;
}
