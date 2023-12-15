export type FrameworkInstructions = {
  name: string;
  baseURL: string;
  repository: string;
  icon: string;
  invert?: boolean;
};

const frameworks: Record<string, FrameworkInstructions> = {
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
    invert: true,
  },
  browser: {
    name: "Browser Extension",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/packages/browser/",
    repository: "https://github.com/aptabase/aptabase-js/tree/main/packages/browser",
    icon: "https://aptabase.com/tools/chrome.svg",
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
    baseURL: "https://raw.githubusercontent.com/goenning/nativescript-plugins/main/packages/nativescript-aptabase",
    repository: "https://github.com/nstudio/nativescript-plugins/blob/main//packages/nativescript-aptabase",
    icon: "https://aptabase.com/tools/nativescript.svg",
  },
  nextjs: {
    name: "Next.js",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/packages/react/",
    repository: "https://github.com/aptabase/aptabase-js/tree/main/packages/react",
    icon: "https://aptabase.com/tools/nextjs.svg",
  },
  maui: {
    name: ".NET MAUI",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-maui/main/",
    repository: "https://github.com/aptabase/aptabase-maui",
    icon: "https://aptabase.com/tools/dotnet.svg",
  },
  unity: {
    name: "Unity Engine",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-unity/main/",
    repository: "https://github.com/aptabase/aptabase-unity",
    icon: "https://aptabase.com/tools/unity.svg",
    invert: true,
  },
  react: {
    name: "React",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/packages/react/",
    repository: "https://github.com/aptabase/aptabase-js/tree/main/packages/react",
    icon: "https://aptabase.com/tools/react.svg",
  },
  "react-native": {
    name: "React Native",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-react-native/main/",
    repository: "https://github.com/aptabase/aptabase-react-native",
    icon: "https://aptabase.com/tools/react-native.svg",
  },
  remix: {
    name: "Remix",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/packages/react/",
    repository: "https://github.com/aptabase/aptabase-js/tree/main/packages/react",
    icon: "https://aptabase.com/tools/remix.svg",
    invert: true,
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
    invert: true,
  },
  webapp: {
    name: "Web App",
    baseURL: "https://raw.githubusercontent.com/aptabase/aptabase-js/main/packages/web/",
    repository: "https://github.com/aptabase/aptabase-js/tree/main/packages/web",
    icon: "https://aptabase.com/tools/javascript.svg",
  },
};

export default frameworks;
