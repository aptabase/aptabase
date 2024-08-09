import { DatePickerSuggest } from "@datepicker-suggest/react";
import { useAtom, useAtomValue } from "jotai";
import { useEffect } from "react";
import {
  endDateAtom,
  endDatePersistentLabelAtom,
  startDateAtom,
  startDatePersistentLabelAtom,
} from "../../atoms/date-atoms";

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

const optionsSuggestions = [
  "Today",
  "Yesterday",
  "24 hours ago",
  "48 hours ago",
  "7 days ago",
  "30 days ago",
  "This month",
  "Last month",
  "3 months ago",
  "6 months ago",
  "12 months ago",
  "This year",
  "Last year",
];

export function DatePickerSuggestPeriod() {
  const [startDate, setStartDate] = useAtom(startDateAtom);
  const startDatePersistentLabel = useAtomValue(startDatePersistentLabelAtom);
  const [endDate, setEndDate] = useAtom(endDateAtom);
  const endDatePersistentLabel = useAtomValue(endDatePersistentLabelAtom);

  useEffect(() => {
    if (!endDate || !startDate) {
      return;
    }
    if (endDate.date.getTime() < startDate.date.getTime()) {
      setEndDate(startDate);
    }
  }, [endDate]);

  useEffect(() => {
    if (!startDate || !endDate) {
      return;
    }
    if (startDate.date.getTime() > endDate.date.getTime()) {
      setStartDate(endDate);
    }
  }, [startDate]);

  return (
    <>
      <div className="flex gap-2">
        <div className="flex flex-col">
          <label className="text-xs font-light mb-1 ml-1 text-gray-700 dark:text-gray-300">Start date</label>
          <DatePickerSuggest
            panelClassName="w-72"
            suggestion={startDate ?? undefined}
            onSuggestionChange={(dateSuggestion) => {
              const newStartDate = dateSuggestion ?? startDate;
              setStartDate(newStartDate);
            }}
            initialSuggestion={startDatePersistentLabel}
            optionsSuggestions={optionsSuggestions}
          />
        </div>
        <div className="flex flex-col">
          <label className="text-xs font-light mb-1 ml-1 text-gray-700 dark:text-gray-300">End date</label>
          <DatePickerSuggest
            panelClassName="w-72"
            onSuggestionChange={(dateSuggestion) => {
              const newEndDate = dateSuggestion ?? endDate;
              setEndDate(newEndDate);
            }}
            initialSuggestion={endDatePersistentLabel}
          />
        </div>
      </div>
    </>
  );
}
