import { ErrorState, LoadingState, api } from "@app/primitives";
import { useQuery } from "@tanstack/react-query";

type CurrentUsageItem = {
  count: number;
  quota: number;
};

export function CurrentUsage() {
  const { isLoading, isError, data } = useQuery(["billing-usage"], () =>
    api.get<CurrentUsageItem>(`/_billing`)
  );

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState />;

  const perc = (data.count / data.quota) * 100;

  return (
    <div className="flex flex-col space-y-1">
      <p className="text-sm font-medium text-gray-900 mb-1">Current Usage</p>
      <div className="text-sm text-secondary">
        {data.count?.toLocaleString()} / {data.quota?.toLocaleString()} events (
        {perc.toPrecision(2)}%)
      </div>
      <div className="overflow-hidden rounded-full bg-gray-200">
        <div
          className="h-2 rounded-full bg-primary"
          style={{ width: `${perc}%` }}
        />
      </div>
    </div>
  );
}
