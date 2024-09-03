import { trackEvent } from "@aptabase/web";
import { Button } from "@components/Button";
import { Popover, PopoverContent, PopoverTrigger } from "@components/Popover";
import { Application, BuildMode } from "@features/apps";
import { formatNumber } from "@fns/format-number";
import { addDays, addMonths, endOfMonth, format, isBefore, min, startOfMonth, subMinutes } from "date-fns";

type Props = {
  app: Application;
  buildMode: BuildMode;
  year: number;
  format: string;
  months: Array<{ number: number; name: string; events: number }>;
};

type WeekWithStartEnd = {
  startDay: Date;
  endDay: Date;
};

const EVENTS_COUNT_WITH_DROPDOWN = 1000000;

export function YearlyGrid(props: Props) {
  const download = (fullMonth?: Props["months"][number], startDate?: Date, endDate?: Date) => () => {
    const params = new URLSearchParams();
    params.set("appId", props.app.id);
    params.set("appName", props.app.name);
    params.set("buildMode", props.buildMode);
    params.set("format", props.format);

    if (fullMonth) {
      startDate = startOfMonth(new Date(props.year, fullMonth.number - 1));
      endDate = endOfMonth(startDate);
    }
    const startDateUTC = new Date(startDate!.getTime() - startDate!.getTimezoneOffset() * 60000);
    const endDateUTC = new Date(endDate!.getTime() - endDate!.getTimezoneOffset() * 60000);
    params.set("startDate", startDateUTC.toISOString());
    params.set("endDate", endDateUTC.toISOString());

    trackEvent("export", { name: props.app.name });

    location.href = `/api/_export/download?${params.toString()}`;
  };

  return (
    <div>
      <p className="font-title text-lg">{props.year}</p>
      <div className="grid grid-cols-3 sm:grid-cols-2 md:grid-cols-6 max-w-fit gap-3">
        {props.months.map((month) =>
          month.events < EVENTS_COUNT_WITH_DROPDOWN ? (
            <Button
              key={month.number}
              variant="secondary"
              className="w-24 flex flex-col px-0 tracking-tighter"
              onClick={download(month)}
            >
              <span className="text-sm font-medium">{month.name}</span>
              <span className="text-xs text-muted-foreground">
                {formatNumber(month.events)} {month.events === 1 ? "event" : "events"}
              </span>
            </Button>
          ) : (
            <Popover>
              <PopoverTrigger className="relative">
                <Button key={month.number} variant="secondary" className="w-24 flex flex-col px-0 tracking-tighter">
                  <span className="text-sm font-medium">{month.name}</span>
                  <span className="text-xs text-muted-foreground">
                    {formatNumber(month.events)} {month.events === 1 ? "event" : "events"}
                  </span>
                </Button>
              </PopoverTrigger>
              <PopoverContent className="p-2 w-60">
                <div className="text-sm space-y-2 text-center">
                  {getWeeks(month.number, props.year).map((week, index) => {
                    const weekLabel = `Week ${index + 1}`;
                    const startEndLabel = `${format(week.startDay, "MMM d")} - ${format(week.endDay, "MMM d")}`;
                    return (
                      <Button
                        key={week.startDay.toISOString()}
                        onClick={download(undefined, week.startDay, week.endDay)}
                        variant="ghost"
                        className="mb-2 w-full flex justify-between"
                      >
                        <span>{weekLabel}</span>
                        <span className="text-sm text-muted-foreground">{startEndLabel}</span>
                      </Button>
                    );
                  })}
                </div>
              </PopoverContent>
            </Popover>
          )
        )}
      </div>
    </div>
  );
}

function getWeeks(monthIndex: number, year: number) {
  const startOfMonthDate = startOfMonth(new Date(year, monthIndex - 1));
  const startOfNextMonth = addMonths(startOfMonthDate, 1);

  let weeks: WeekWithStartEnd[] = [];
  for (let i = startOfMonthDate; isBefore(i, startOfNextMonth); i = addDays(i, 7)) {
    const startDay = i;
    const endDay = min([addDays(i, 7), subMinutes(startOfNextMonth, 1)]);
    weeks.push({ startDay, endDay });
  }

  return weeks;
}
