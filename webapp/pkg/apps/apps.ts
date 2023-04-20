import { api } from "@app/primitives";
import { trackEvent } from "@aptabase/web";

export type Application = {
  id: string;
  name: string;
  appKey: string;
};

export async function listApps(): Promise<Application[]> {
  return await api.get<Application[]>("/_apps");
}

export async function createApp(name: string): Promise<Application> {
  const app = await api.post<Application>("/_apps", { name });
  trackEvent("app_created");

  return app;
}

export async function updateApp(appId: string, name: string): Promise<Application> {
  return await api.put<Application>(`/_apps/${appId}`, { name });
}

export async function deleteApp(appId: string): Promise<void> {
  return await api.delete(`/_apps/${appId}`);
}
