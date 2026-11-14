import { getOptional } from "@fns/query-api";
import { useQuery } from "@tanstack/react-query";

export type Grant = {
  id: string;
  clientName: string;
  clientUrl: string;
  verified: boolean;
  scopes: string[];
  allApps: boolean;
  apps: { id: string; name: string }[];
  consentedAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
  revokedReason: string | null;
};

// data is null when the feature is switched off on the server
export function useGrants() {
  return useQuery({
    queryKey: ["oauth-grants"],
    queryFn: () => getOptional<Grant[]>("/_oauth/grants"),
    retry: false,
  });
}
