import { api } from "@fns/api";
import { trackEvent } from "@aptabase/web";

export type RetentionMonths = 6 | 12 | 24 | 60;

export type Application = {
  id: string;
  name: string;
  appKey: string;
  iconPath: string;
  hasEvents: boolean;
  hasOwnership: boolean;
  lockReason?: string;
  retentionMonths: RetentionMonths;
};

export async function listApps(): Promise<Application[]> {
  return await api.get<Application[]>("/_apps");
}

export async function getAppById(appId: string): Promise<Application> {
  return await api.get(`/_apps/${appId}`);
}

export async function createApp(name: string, retentionMonths: RetentionMonths = 60): Promise<Application> {
  const app = await api.post<Application>("/_apps", { name, retentionMonths });
  trackEvent("app_created", { name });

  return app;
}

export async function updateApp(appId: string, name: string, icon: string, retentionMonths?: RetentionMonths): Promise<Application> {
  return await api.put<Application>(`/_apps/${appId}`, { name, icon, retentionMonths });
}

export async function deleteApp(appId: string): Promise<void> {
  return await api.delete(`/_apps/${appId}`);
}
