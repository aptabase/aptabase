const regions: { [host: string]: "eu" | "us" } = {
  "us.aptabase.com": "us",
  "eu.aptabase.com": "eu",
};

export const isDevelopment = import.meta.env.DEV;
export const isManagedCloud = !!regions[window.location.hostname];
export const isBillingEnabled = isManagedCloud || isDevelopment;
export const region = regions[window.location.hostname];
