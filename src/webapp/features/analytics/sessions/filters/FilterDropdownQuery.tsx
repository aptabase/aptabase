import { QueryParams, TopNItem, TopNQueryChildrenProps } from "@features/analytics/query";
import { useApps } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { startOfDay, subMonths, subWeeks } from "date-fns";

type Props = {
  appId: string;
  queryKey: string;
  query: (params: QueryParams) => Promise<TopNItem[]>;
  children: (props: TopNQueryChildrenProps) => JSX.Element;
};

export function FilterDropdownQuery(props: Props) {
  const { buildMode } = useApps();

  const startDateIso = subWeeks(startOfDay(new Date()), 1).toISOString();
  const startDateOneMonthBefore = subMonths(startOfDay(new Date()), 1).toISOString();
  const endDateIso = "";

  const { isLoading, isError, data, refetch } = useQuery({
    queryKey: [props.queryKey, buildMode, props.appId, startDateIso, endDateIso],
    queryFn: async () => {
      const oneWeekResult = await props.query({
        buildMode,
        appId: props.appId,
        startDate: startDateIso,
        endDate: endDateIso,
      });

      if (oneWeekResult.filter((i) => i.name).length === 0) {
        const items = await props.query({
          buildMode,
          appId: props.appId,
          startDate: startDateOneMonthBefore,
          endDate: endDateIso,
        });

        return items.filter((i) => i.name);
      }

      return oneWeekResult.filter((i) => i.name);
    },
    staleTime: 10000,
  });

  return props.children({ isLoading, isError, items: data || [], refetch });
}
