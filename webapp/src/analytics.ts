import { init } from "@aptabase/web";

const devAppKey = "A-DEV-0338260222";
const appKeys: { [host: string]: string } = {
  "us.aptabase.com": "A-US-9580647299",
  "eu.aptabase.com": "A-EU-2458276968",
};

const appKey = appKeys[window.location.hostname] ?? devAppKey;

export function initAnalytics() {
  init(appKey, {
    appVersion: import.meta.env.APP_VERSION,
  });
}
