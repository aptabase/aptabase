import { Button } from "@components/Button";
import { Application, BuildMode } from "@features/apps";
import { formatNumber } from "@fns/format-number";
import { trackEvent } from "@aptabase/web";

type Props = {
  app: Application;
  buildMode: BuildMode;
  year: number;
  months: Array<{ number: number; name: string; events: number }>;
};

export function YearlyGrid(props: Props) {
  const donload = (month: number) => () => {
    const params = new URLSearchParams();
    params.set("appId", props.app.id);
    params.set("appName", props.app.name);
    params.set("buildMode", props.buildMode);
    params.set("year", props.year.toString());
    params.set("month", month.toString());

    trackEvent("export", { name: props.app.name });

    location.href = `/api/_export/download?${params.toString()}`;
  };

  return (
    <div className="space-y-1">
      <p className="font-title text-xl">{props.year}</p>
      <div className="grid grid-cols-3 sm:grid-cols-2 md:grid-cols-6 max-w-fit gap-3">
        {props.months.map((month) => (
          <Button
            key={month.number}
            variant="secondary"
            onClick={donload(month.number)}
            className="w-24 flex flex-col px-0"
          >
            <span className="text-sm font-medium">{month.name}</span>
            <span className="text-xs text-muted-foreground">
              {formatNumber(month.events)} {month.events === 1 ? "event" : "events"}
            </span>
          </Button>
        ))}
      </div>
    </div>
  );
}
