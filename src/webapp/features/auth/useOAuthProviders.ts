import { isManagedCloud } from "@features/env";
import { useQuery } from "@tanstack/react-query";
import { OAuthProviders, getOAuthProviders } from "./auth";

// Managed cloud always has both providers configured, so seed the query with
// initialData to avoid a flicker (or hiding the buttons if the request fails).
// Self-hosted instances ask the server which providers are configured.
export function useOAuthProviders(): OAuthProviders {
  const { data } = useQuery({
    queryKey: ["oauthProviders"],
    queryFn: getOAuthProviders,
    staleTime: Infinity,
    initialData: isManagedCloud ? { github: true, google: true } : undefined,
  });

  return data ?? { github: false, google: false };
}
