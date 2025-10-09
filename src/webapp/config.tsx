import { createContext, ReactNode, useState, useContext, useEffect } from "react";
import { api } from "@fns/api";

export type Config = {
  disableSignup: boolean
};

export type ConfigContextType = {
  configIsLoading: boolean;
  config: Config;
  refresh: () => Promise<void>;
};

const ConfigContext = createContext<ConfigContextType>({
  configIsLoading: false,
  config: {
    disableSignup: false
  },
  refresh: () => Promise.resolve()
});

export function ConfigProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<Config>({
    disableSignup: false
  });
  const [configIsLoading, setIsLoading] = useState(false);

  useEffect(() => {
    refresh();
  }, []);

  return <ConfigContext.Provider value={{ configIsLoading, config, refresh }}>{children}</ConfigContext.Provider>;
  
  async function refresh() {
    setIsLoading(true);

    try {
      const [status, config] = await api.fetch('GET', '/_config');
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