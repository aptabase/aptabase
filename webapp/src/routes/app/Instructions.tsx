import { useCurrentApp } from "@app/navigation";
import {
  ErrorState,
  Head,
  LoadingState,
  Markdown,
  PageHeading,
} from "@app/primitives";
import { trackEvent } from "@aptabase/web";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";

type FrameworkInstructions = {
  name: string;
  readme?: string;
  repository: string;
};

const frameworks: { [id: string]: FrameworkInstructions } = {
  android: {
    name: "Android (Kotlin)",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-kotlin/main/README.md",
    repository: "https://github.com/aptabase/aptabase-kotlin",
  },
  swift: {
    name: "Apple (Swift)",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-swift/main/README.md",
    repository: "https://github.com/aptabase/aptabase-swift",
  },
  capacitor: {
    name: "Capacitor.js",
    repository: "https://github.com/aptabase/aptabase/issues/6",
  },
  electron: {
    name: "Electron",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-electron/main/README.md",
    repository: "https://github.com/aptabase/aptabase-electron",
  },
  flutter: {
    name: "Flutter",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase_flutter/main/README.md",
    repository: "https://github.com/aptabase/aptabase_flutter",
  },
  nativescript: {
    name: "NativeScript",
    readme:
      "https://raw.githubusercontent.com/goenning/nativescript-plugins/main/packages/nativescript-aptabase/README.md",
    repository:
      "https://github.com/nstudio/nativescript-plugins/blob/main/packages/nativescript-aptabase",
  },
  maui: {
    name: ".NET MAUI",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-maui/main/README.md",
    repository: "https://github.com/aptabase/aptabase-maui",
  },
  "react-native": {
    name: "React Native",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-react-native/main/README.md",
    repository: "https://github.com/aptabase/aptabase-react-native",
  },
  tauri: {
    name: "Tauri",
    readme:
      "https://raw.githubusercontent.com/aptabase/tauri-plugin-aptabase/main/README.md",
    repository: "https://github.com/aptabase/tauri-plugin-aptabase",
  },
  unity: {
    name: "Unity",
    repository: "https://github.com/aptabase/aptabase/issues/10",
  },
  webapp: {
    name: "Web App",
    readme:
      "https://raw.githubusercontent.com/aptabase/aptabase-js/main/README.md",
    repository: "https://github.com/aptabase/aptabase-js",
  },
};

const fetchInstructions = async (id: string): Promise<[string, string]> => {
  const fw = frameworks[id];
  if (!fw) {
    return ["", ""];
  }

  trackEvent("instructions_viewed", { framework: id });

  if (fw.readme) {
    const response = await fetch(fw.readme);
    const content = await response.text();
    return [content, fw.repository];
  }

  return [
    `
  # ${fw.name} for Aptabase
  
  We don't have an SDK for ${fw.name} at this time.
  
  If you're insterested in using Aptabase with ${fw.name}, please upvote [this issue](${fw.repository}) on GitHub.

  `,
    fw.repository,
  ];
};

Component.displayName = "Instructions";
export function Component() {
  const app = useCurrentApp();
  const [selected, setSelected] = useState("");

  const { isLoading, isError, data } = useQuery(["markdown", selected], () =>
    fetchInstructions(selected)
  );
  const [content, repository] = data || [];

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSelected(e.target.value);
  };

  const availableFws = Object.entries(frameworks).filter(([, fw]) => fw.readme);
  const comingSoonFws = Object.entries(frameworks).filter(
    ([, fw]) => !fw.readme
  );

  return (
    <>
      <Head title={`${app.name} - Instructions`} />
      <PageHeading
        title="Instructions"
        subtitle="Instrument your app with our SDK"
      />
      <div className="flex flex-col space-y-8 mt-8">
        <div className="px-4 py-2 bg-muted max-w-fit rounded border border-default">
          <p className="text-subtle text-sm mb-1">
            👇 App key for <span className="text-inverted">{app.name}</span>
          </p>
          <span className="font-medium text-xl mb-2">{app.appKey} </span>
          <p className="text-subtle text-sm mt-2">
            It is used by the SDK to identify your app
          </p>
        </div>

        <div className="flex items-center border-b pb-4 border-default justify-between">
          <div className="flex items-center space-x-4">
            <span className="whitespace-nowrap">
              Which framework are you using?
            </span>
            <select
              name="framework"
              defaultValue={selected}
              onChange={handleChange}
              className="form-select max-w-fit"
            >
              <option value="" disabled>
                Framework
              </option>
              {availableFws.map(([id, fw]) => (
                <option key={fw.name} value={id}>
                  {fw.name}
                </option>
              ))}
              <option value="" disabled>
                Coming soon
              </option>
              {comingSoonFws.map(([id, fw]) => (
                <option key={id} value={id}>
                  {fw.name}
                </option>
              ))}
            </select>
          </div>
          {repository && (
            <a
              target="_blank"
              className="hidden md:flex hover:bg-muted text-sm rounded p-2 items-center space-x-1"
              href={repository}
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
              content={(content || "").replace("<YOUR_APP_KEY>", app.appKey)}
            />
          </div>
        )}
      </div>
    </>
  );
}
