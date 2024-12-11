import { api } from "@fns/api";
import { QueryObserverResult, RefetchOptions } from "@tanstack/react-query";

export type TopNItem = {
  name: string;
  value: number;
};

export type QueryParams = {
  buildMode: "release" | "debug";
  appId: string;
  sessionId?: string;
  startDate?: string;
  endDate?: string;
  granularity?: Granularity;
  countryCode?: string;
  osName?: string;
  eventName?: string;
  appVersion?: string;
};

export type TopNQueryChildrenProps = {
  isLoading: boolean;
  isError: boolean;
  items: TopNItem[];
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<TopNItem[], Error>>;
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

export type KeyMetrics = {
  current: {
    dailyUsers: number;
    sessions: number;
    events: number;
    durationSeconds: number;
  };
  previous?: {
    dailyUsers: number;
    sessions: number;
    events: number;
    durationSeconds: number;
  };
};

export function keyMetrics(params: QueryParams): Promise<KeyMetrics> {
  return api.get<KeyMetrics>(`/_stats/metrics`, params);
}

export type Granularity = "hour" | "day" | "month";

export type PeriodicStats = Array<{
  period: string;
  users: number;
  sessions: number;
  events: number;
}>;

export function periodicStats(params: QueryParams): Promise<PeriodicStats> {
  return api.get<PeriodicStats>(`/_stats/periodic`, params);
}

type EventPropsItem = {
  stringKey: string;
  stringValue: string;
  numericKey: string;
  events: number;
  median: number;
  min: number;
  max: number;
  sum: number;
};

export function topEventProps(params: QueryParams): Promise<EventPropsItem[]> {
  return api.get<EventPropsItem[]>(`/_stats/top-props`, params);
}

type LiveGeoDataPoint = {
  countryCode: string;
  regionName: string;
  latitude: number;
  longitude: number;
  users: number;
};

export function liveGeoDataPoints(params: QueryParams): Promise<LiveGeoDataPoint[]> {
  return api.get<LiveGeoDataPoint[]>(`/_stats/live-geo`, params);
}

export type LiveRecentSession = {
  id: string;
  eventsCount: number;
  startedAt: string;
  duration: number;
  appVersion: string;
  countryCode: string;
  regionName: string;
  osName: string;
  osVersion: string;
};

export function liveRecentSessions(params: QueryParams): Promise<LiveRecentSession[]> {
  return api.get<LiveRecentSession[]>(`/_stats/live-sessions`, params);
}

export function historicalSessions(params: QueryParams): Promise<LiveRecentSession[]> {
  return api.get<LiveRecentSession[]>(`/_stats/historical-sessions`, params);
}

export type SessionTimeline = {
  eventsName: string[];
  eventsTimestamp: string[];
  eventsStringProps: string[];
  eventsNumericProps: string[];
};

export function liveSessionDetails(params: QueryParams): Promise<LiveRecentSession & SessionTimeline> {
  return api.get<LiveRecentSession & SessionTimeline>(`/_stats/live-session-details`, params);
}
