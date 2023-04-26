import { init } from "@aptabase/web";
import { isDevelopment, isManagedCloud, region } from "@app/env";

const devAppKey = "A-DEV-0000000000";
const appKeys: { [host: string]: string } = {
  us: "A-US-9580647299",
  eu: "A-EU-2458276968",
};

const appKey = isManagedCloud
  ? appKeys[region]
  : isDevelopment
  ? devAppKey
  : undefined;

export function initAnalytics() {
  init(appKey ?? "", {
    appVersion: import.meta.env.APP_VERSION,
  });
}
