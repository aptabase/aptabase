import { useAtomValue } from "jotai";
import { periodAtom } from "../../../atoms/date-atoms";
import { DatePickerSuggestPeriod } from "./DatePickerSuggestPeriod";
import { DateRangePicker } from "./DateRangePicker";

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
  custom: "custom",
};

export function DateFilterContainer() {
  const period = useAtomValue(periodAtom);

  return period === PERIOD_ENUM.custom || !period ? <DatePickerSuggestPeriod /> : <DateRangePicker />;
}
