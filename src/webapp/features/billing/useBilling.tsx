import { UseQueryResult, useQuery } from "@tanstack/react-query";
import { api } from "@fns/api";
import { isBillingEnabled } from "@features/env";

export type BillingInfo = {
  usage: number;
  month: number;
  year: number;
  state: "OK" | "OVERUSE";
  subscription?: {
    status: string;
    endsAt: string;
  };
  plan: {
    name: string;
    monthlyPrice: number;
    monthlyEvents: number;
  };
};

export type BillingHistoricalUsage = {
  date: string;
  events: number;
};

export function useBilling(): UseQueryResult<BillingInfo> {
  return useQuery({ queryKey: ["billing"], queryFn: () => api.get<BillingInfo>(`/_billing`), refetchOnMount: true });
}

export function useHistoricalData(): UseQueryResult<BillingHistoricalUsage[]> {
  return useQuery({
    queryKey: ["billing-historical"],
    queryFn: () => api.get<BillingHistoricalUsage[]>(`/_billing/historical`),
  });
}

export function useBillingState(): "OK" | "OVERUSE" {
  if (!isBillingEnabled) return "OK";

  const { data } = useBilling();
  return data?.state ?? "OK";
}
