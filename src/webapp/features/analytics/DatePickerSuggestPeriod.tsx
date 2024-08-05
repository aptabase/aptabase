import { DatePickerSuggest } from "@datepicker-suggest/react";
import { useDatePicker } from "@hooks/use-datepicker";

type Option = {
  value: string;
  name: string;
};

export const PERIOD_ENUM = {
  "24h": "24h",
  "48h": "48h",
  today: "today",
  yesterday: "yesterday",
  "7d": "7d",
  "14d": "14d",
  "30d": "30d",
  month: "month",
  "last-month": "last-month",
  "90d": "90d",
  "180d": "180d",
  "365d": "365d",
  all: "all",
};

const options: Option[] = [
  { value: "today", name: "Today" },
  { value: "yesterday", name: "Yesterday" },
  { value: "divider-6", name: "Divider" },
  { value: "24h", name: "Last 24 hours" },
  { value: "48h", name: "Last 48 hours" },
  { value: "divider-2", name: "Divider" },
  { value: "7d", name: "Last 7 days" },
  { value: "14d", name: "Last 14 days" },
  { value: "30d", name: "Last 30 days" },
  { value: "divider-1", name: "Divider" },
  { value: "month", name: "This month" },
  { value: "last-month", name: "Last month" },
  { value: "divider-3", name: "Divider" },
  { value: "90d", name: "Last 3 months" },
  { value: "180d", name: "Last 6 months" },
  { value: "365d", name: "Last 12 months" },
  { value: "divider-4", name: "Divider" },
  { value: "all", name: "All time" },
];

type StyledOptionProps = {
  option: Option;
};

export function DatePickerSuggestPeriod() {
  const { startDate, endDate, setStartEndDate } = useDatePicker();

  return (
    <>
      <div className="flex">
        <DatePickerSuggest
          value={startDate}
          panelClassName="w-72"
          onChange={(dateUpdate) => {
            const newStartDate = dateUpdate ?? startDate;
            setStartEndDate({ startDate: newStartDate, endDate: endDate });
          }}
        />
        <DatePickerSuggest
          panelClassName="w-72"
          value={endDate}
          onChange={(dateUpdate) => {
            const newEndDate = dateUpdate ?? endDate;
            setStartEndDate({ endDate: newEndDate, startDate: startDate });
          }}
        />
      </div>
    </>
  );
}
