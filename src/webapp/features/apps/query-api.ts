import { getOptional } from "@fns/query-api";
import { useQuery } from "@tanstack/react-query";

export type ServiceClient = {
  clientId: string;
  name: string;
  scopes: string[];
  secretPrefix: string;
  createdAt: string;
  lastUsedAt: string | null;
  deniedCount: number;
  revokedAt: string | null;
  revokedReason: string | null;
};

export type ServiceClientList = {
  limit: number | null;
  clients: ServiceClient[];
};

export type AppApiUsage = {
  isOwner: boolean;
  allowance: number | null;
  used: number;
  remaining: number | null;
  resetsAt: string;
  clients: {
    grantId: string;
    kind: "user" | "service";
    clientName: string;
    person: string;
    requests: number;
    rejected: number;
    lastUsedAt: string | null;
    revoked: boolean;
  }[];
};

// In both hooks data is null when the feature is switched off on the server
// or the signed-in user may not see the resource.

export function useServiceClients(appId: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ["service-clients", appId],
    queryFn: () => getOptional<ServiceClientList>(`/_apps/${appId}/clients`),
    enabled: enabled && !!appId,
    retry: false,
  });
}

export function useAppApiUsage(appId: string | undefined, enabled: boolean = true) {
  return useQuery({
    queryKey: ["app-api-usage", appId],
    queryFn: () => getOptional<AppApiUsage>(`/_apps/${appId}/api-usage`),
    enabled: enabled && !!appId,
    retry: false,
  });
}
