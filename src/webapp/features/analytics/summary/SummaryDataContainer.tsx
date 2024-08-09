import { useQuery } from "@tanstack/react-query";
import { Granularity, KeyMetrics, keyMetrics, periodicStats } from "../query";

type ChildrenProps = {
  dailyUsers?: number[];
  metrics?: KeyMetrics;
};

type Props = {
  appId: string;
  startDate: string | undefined;
  endDate: string | undefined;
  granularity: Granularity;
  buildMode: "release" | "debug";
  children: (props: ChildrenProps) => JSX.Element;
};

export function SummaryDataContainer(props: Props) {
  const { data: metrics } = useQuery({
    queryKey: ["key-metrics", props.appId, props.buildMode, props.startDate, props.endDate],
    queryFn: () =>
      keyMetrics({
        buildMode: props.buildMode,
        appId: props.appId,
        startDate: props.startDate,
        endDate: props.endDate,
        granularity: props.granularity,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }),
    staleTime: 60000,
    enabled: !!props.startDate && !!props.endDate,
  });

  const { data: dailyUsers } = useQuery({
    queryKey: ["periodic-stats", props.appId, props.buildMode, props.startDate, props.endDate],
    queryFn: () =>
      periodicStats({
        buildMode: props.buildMode,
        appId: props.appId,
        startDate: props.startDate,
        endDate: props.endDate,
        granularity: props.granularity,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }).then((s) => s.map((x) => x.users)),
    staleTime: 60000,
    enabled: !!props.startDate && !!props.endDate,
  });

  return props.children({ dailyUsers, metrics });
}
