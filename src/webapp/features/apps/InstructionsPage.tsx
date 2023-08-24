import { useCurrentApp } from "@features/apps";
import {
  ErrorState,
  LoadingState,
  Markdown,
  PageHeading,
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectGroup,
  SelectItem,
  Page,
} from "@features/primitives";
import { trackEvent } from "@aptabase/web";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Navigate } from "react-router-dom";

type FrameworkInstructions = {
  name: string;
  baseURL: string;
  repository: string;
  icon: string;
};

const frameworks: { [id: string]: FrameworkInstructions } = {
  android: {
    name: "Android (Kotlin)",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-kotlin/main/",
    repository: "https://github.com/aptabase/aptabase-kotlin",
    icon: "https://aptabase.com/tools/android.svg",
  },
  swift: {
    name: "Apple (Swift)",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-swift/main/",
    repository: "https://github.com/aptabase/aptabase-swift",
    icon: "https://aptabase.com/tools/apple.svg",
  },
  electron: {
    name: "Electron",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-electron/main/",
    repository: "https://github.com/aptabase/aptabase-electron",
    icon: "https://aptabase.com/tools/electron.svg",
  },
  flutter: {
    name: "Flutter",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase_flutter/main/",
    repository: "https://github.com/aptabase/aptabase_flutter",
    icon: "https://aptabase.com/tools/flutter.svg",
  },
  nativescript: {
    name: "NativeScript",
    baseURL:
      "https://raw.githubusercontent.com/goenning/nativescript-plugins/main//packages/nativescript-aptabase",
    repository:
      "https://github.com/nstudio/nativescript-plugins/blob/main//packages/nativescript-aptabase",
    icon: "https://aptabase.com/tools/nativescript.svg",
  },
  maui: {
    name: ".NET MAUI",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-maui/main/",
    repository: "https://github.com/aptabase/aptabase-maui",
    icon: "https://aptabase.com/tools/dotnet.svg",
  },
  "react-native": {
    name: "React Native",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-react-native/main/",
    repository: "https://github.com/aptabase/aptabase-react-native",
    icon: "https://aptabase.com/tools/react-native.svg",
  },
  tauri: {
    name: "Tauri",
    baseURL: "https://raw.githubusercontent.com/aptabase/tauri-plugin-aptabase/main/",
    repository: "https://github.com/aptabase/tauri-plugin-aptabase",
    icon: "https://aptabase.com/tools/tauri.svg",
  },
  unreal: {
    name: "Unreal Engine",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-unreal/main/",
    repository: "https://github.com/aptabase/aptabase-unreal",
    icon: "https://aptabase.com/tools/unreal.svg",
  },
  webapp: {
    name: "Web App",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/",
    repository: "https://github.com/aptabase/aptabase-js",
    icon: "https://aptabase.com/tools/javascript.svg",
  },
};

const fetchInstructions = async (id: string): Promise<[FrameworkInstructions, string]> => {
  const fw = frameworks[id];
  if (!fw) {
    return [fw, ""];
  }

  trackEvent("instructions_viewed", { framework: id });

  if (fw.baseURL) {
    const response = await fetch(`${fw.baseURL}/README.md`);
    const content = await response.text();
    return [fw, content];
  }

  return [
    fw,
    `
  # ${fw.name} for Aptabase
  
  We don't have an SDK for ${fw.name} at this time.
  
  If you're insterested in using Aptabase with ${fw.name}, please upvote [this issue](${fw.repository}) on GitHub.

  `,
  ];
};

Component.displayName = "InstructionsPage";
export function Component() {
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  const [selected, setSelected] = useState("");

  const { isLoading, isError, data } = useQuery(["markdown", selected], () =>
    fetchInstructions(selected)
  );

  const [fw, content] = data || [];

  return (
    <Page title={`${app.name} - Instructions`}>
      <PageHeading title="Instructions" subtitle="Instrument your app with our SDK" />
      <div className="flex flex-col space-y-8 mt-8">
        <div className="px-4 py-2 bg-muted max-w-fit rounded">
          <p className="text-muted-foreground text-sm mb-1">
            App Key for <span className="text-foreground">{app.name}</span>
          </p>
          <span className="font-medium text-xl mb-2">{app.appKey} </span>
          <p className="text-muted-foreground text-sm mt-2">
            It is used by the SDK to identify your app
          </p>
        </div>

        <div className="flex items-center border-b pb-4 justify-between">
          <div className="flex items-center space-x-4">
            <Select onValueChange={setSelected}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Select a framework" />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  {Object.entries(frameworks).map(([id, fw]) => (
                    <SelectItem key={fw.name} value={id}>
                      <div className="flex gap-2 items-center">
                        <img src={fw.icon} className="h-4 w-4" />
                        <span>{fw.name}</span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
          {fw?.repository && (
            <a
              target="_blank"
              className="hidden md:flex hover:bg-accent text-sm rounded p-2 items-center space-x-1"
              href={fw?.repository}
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-4 w-4 fill-current"
                viewBox="0 0 24 24"
              >
                <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z" />
              </svg>
              <span>View on GitHub</span>
            </a>
          )}
        </div>

        {isLoading ? (
          <LoadingState />
        ) : isError ? (
          <ErrorState />
        ) : (
          <div>
            <Markdown
              baseURL={fw?.baseURL ?? ""}
              content={(content || "").replace("<YOUR_APP_KEY>", app.appKey)}
            />
          </div>
        )}
      </div>
    </Page>
  );
}
