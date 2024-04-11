import { useQuery } from "@tanstack/react-query";
import { KeyMetrics, keyMetrics, periodicStats } from "../query";

type ChildrenProps = {
  dailyUsers?: number[];
  metrics?: KeyMetrics;
};

type Props = {
  appId: string;
  period: { from: string; to: string };
  buildMode: "release" | "debug";
  children: (props: ChildrenProps) => JSX.Element;
};

export function SummaryDataContainer(props: Props) {
  const { data: metrics } = useQuery({
    queryKey: ["key-metrics", props.appId, props.buildMode, props.period],
    queryFn: () =>
      keyMetrics({
        buildMode: props.buildMode,
        appId: props.appId,
        period: props.period,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }),
    staleTime: 60000,
  });

  const { data: dailyUsers } = useQuery({
    queryKey: ["periodic-stats", props.appId, props.buildMode, props.period],
    queryFn: () =>
      periodicStats({
        buildMode: props.buildMode,
        appId: props.appId,
        period: props.period,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }).then((s) => s.rows.map((x) => x.users)),
    staleTime: 60000,
  });

  return props.children({ dailyUsers, metrics });
}
