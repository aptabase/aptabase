import { ReactNode, createContext, useContext, useEffect, useState } from "react";

export type Theme = "light" | "dark";

const updateTheme = (theme: Theme) => {
  document.body.setAttribute("data-theme", theme);
  const metaTheme = document.querySelector("meta[name=theme-color]");
  if (metaTheme) {
    metaTheme.setAttribute("content", theme === "dark" ? "#000000" : "#ffffff");
  }
};

const preferTheme = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
const defaultTheme = (localStorage.getItem("theme") as Theme) ?? preferTheme;
updateTheme(defaultTheme);

type ContextType = {
  theme: Theme;
  toggleTheme: () => void;
};

const Context = createContext<ContextType>({
  theme: defaultTheme,
  toggleTheme: () => {},
});

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(defaultTheme);

  const toggleTheme = () => {
    const newTheme = theme === "dark" ? "light" : "dark";
    localStorage.setItem("theme", newTheme);
    updateTheme(newTheme);
    setTheme(newTheme);
  };

  return <Context.Provider value={{ theme, toggleTheme }}>{children}</Context.Provider>;
}

export function useTheme(): ContextType {
  return useContext(Context);
}
