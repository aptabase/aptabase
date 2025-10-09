import { createContext, ReactNode, useState, useContext, useEffect } from "react";
import { api } from "@fns/api";

export type Config = {
  disableSignup: boolean
};

export type ConfigContextType = {
  isLoading: boolean;
  config: Config;
  refresh: () => Promise<void>;
};

const ConfigContext = createContext<ConfigContextType>();

export function ConfigProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<Config>({
    disableSignup: false
  });
  const [configIsLoading, setIsLoading] = useState(false);

  useEffect(() => {
    refresh();
  }, []);

  return <ConfigContext.Provider value={{ config, configIsLoading, refresh }}>{children}</ConfigContext.Provider>;
  
  async function refresh() {
    setIsLoading(true);

    try {
      const [status, config] = await api.fetch('GET', '/config');
      setConfig(await config.json());
    }
    catch (ex) {
      console.error(ex);
    }

    setIsLoading(false);
  }
}
export function useConfig() {
  return useContext(ConfigContext);
}