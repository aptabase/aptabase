import { Application, BuildMode } from "@features/apps";
import { YearlyGrid } from "./YearlyGrid";
import { useQuery } from "@tanstack/react-query";
import { api } from "@fns/api";
import { LoadingState } from "@components/LoadingState";
import { ErrorState } from "@components/ErrorState";
import { EmptyState } from "@components/EmptyState";
import { ToolsList } from "./ToolsList";

type Props = {
  app: Application;
  buildMode: BuildMode;
};

type MonthlyUsage = {
  year: number;
  month: number;
  events: number;
};

function getMonthName(number: number) {
  const months = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];
  return months[number - 1];
}

function groupByYear(usage: MonthlyUsage[]) {
  return usage.reduce((result, item) => {
    const existingYear = result.find((entry) => entry.year === item.year);
    const monthName = getMonthName(item.month);

    if (existingYear) {
      existingYear.months.push({ number: item.month, name: monthName, events: item.events });
    } else {
      result.push({
        year: item.year,
        months: [{ number: item.month, name: monthName, events: item.events }],
      });
    }

    return result;
  }, [] as Array<{ year: number; months: Array<{ number: number; name: string; events: number }> }>);
}

export function ExportPageBody(props: Props) {
  const { isLoading, isError, data } = useQuery({
    queryKey: ["monthly-usage", props.app.id, props.buildMode],
    queryFn: () => api.get<MonthlyUsage[]>(`/_export/usage`, { appId: props.app.id, buildMode: props.buildMode }),
  });

  if (isLoading) return <LoadingState size="lg" color="primary" delay={0} />;
  if (isError || !data) return <ErrorState />;

  if (data.length === 0) {
    return (
      <div className="h-40">
        <EmptyState />
      </div>
    );
  }

  const byYear = groupByYear(data);

  return (
    <>
      <div className="space-y-8">
        {byYear.map((item) => (
          <YearlyGrid key={item.year} app={props.app} buildMode={props.buildMode} {...item} />
        ))}
      </div>

      <div className="mt-20">
        <ToolsList />
      </div>
    </>
  );
}
