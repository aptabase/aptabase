import { api } from "@app/primitives";

type TopNItem = {
  name: string;
  value: number;
};

type QueryParams = {
  buildMode: "release" | "debug";
  appId: string;
  period?: string;
  countryCode?: string;
  osName?: string;
  eventName?: string;
  appVersion?: string;
};

export function topCountries(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-countries`, params);
}

export function topRegions(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-regions`, params);
}

export function topOperatingSystem(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-operatingsystems`, params);
}

export function topOSVersions(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-osversions`, params);
}

export function topEvents(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-events`, params);
}

export function topAppVersions(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-appversions`, params);
}

export function topAppBuildNumbers(params: QueryParams): Promise<TopNItem[]> {
  return api.get<TopNItem[]>(`/_stats/top-appbuildnumbers`, params);
}

type KeyMetrics = {
  sessions: number;
  events: number;
  durationSeconds: number;
};

export function keyMetrics(params: QueryParams): Promise<KeyMetrics> {
  return api.get<KeyMetrics>(`/_stats/metrics`, params);
}

export type Granularity = "hour" | "day" | "month";

type PeriodicStats = {
  granularity: Granularity;
  rows: {
    period: string;
    sessions: number;
    events: number;
  }[];
};

export function periodicStats(params: QueryParams): Promise<PeriodicStats> {
  return api.get<PeriodicStats>(`/_stats/periodic`, params);
}

type EventPropsItem = {
  key: string;
  value: string;
  events: number;
};

export function topEventProps(params: QueryParams): Promise<EventPropsItem[]> {
  return api.get<EventPropsItem[]>(`/_stats/top-props`, params);
}
