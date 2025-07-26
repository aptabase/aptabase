const regions: { [host: string]: "eu" | "us" } = {
  "us.aptabase.com": "us",
  "eu.aptabase.com": "eu",
};

function getUserHourCycle(): "h12" | "h24" {
  try {
    const localeCode =
      navigator.languages && navigator.languages.length ? navigator.languages[0] : navigator.language || "en-US";
    const locale = new Intl.Locale(localeCode);

    // TypeScript doesn't know about hourCycles yet
    const cycle = locale.hourCycle ?? (locale as any).hourCycles?.[0];
    if (cycle === "h23" || cycle === "h24") {
      return "h24";
    }
  } catch {
    // it's ok, maybe the browser doesn't support Intl.Locale yet
  }

  return "h12";
}

export const hourCycle = getUserHourCycle();
export const isDevelopment = import.meta.env.DEV;
export const region: string | undefined = regions[window.location.hostname];
export const isManagedCloud = !!region;
export const isOAuthEnabled = isManagedCloud || isDevelopment;
export const isBillingEnabled = isManagedCloud || isDevelopment;
export const isSupportEnabled = isManagedCloud || isDevelopment;
