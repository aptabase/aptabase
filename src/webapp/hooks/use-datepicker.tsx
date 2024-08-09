import { DateSuggestion, SuggestionEngine } from "@datepicker-suggest/core";
import { PERIOD_ENUM } from "@features/analytics/DateRangePicker";
import { Granularity } from "@features/analytics/query";
import {
  differenceInDays,
  differenceInHours,
  endOfDay,
  endOfMinute,
  endOfMonth,
  startOfDay,
  startOfHour,
  startOfMinute,
  startOfMonth,
  sub,
} from "date-fns";
import { useSearchParams } from "react-router-dom";

const setStoredValue = (value: string, key: string) => window.localStorage.setItem(`aptabase-${key}`, value);
const getStoredValue = (key: string) => {
  const dateStr = window.localStorage.getItem(`aptabase-${key}`);
  return dateStr ? dateStr : null;
};
const getStoredPeriod = () => window.localStorage.getItem("period");

export function useDatePicker(): {
  startDate: DateSuggestion;
  endDate: DateSuggestion;
  granularity: Granularity;
  startDateIso: string;
  endDateIso: string;
  setStartDate: (value: DateSuggestion) => void;
  setEndDate: (value: DateSuggestion) => void;
} {
  let [searchParams, setSearchParams] = useSearchParams();
  let startDateLabel = searchParams.get("startDate") ?? getStoredValue("startDate");
  let endDateLabel = searchParams.get("endDate") ?? getStoredValue("endDate");

  const period = searchParams.get("period") ?? getStoredPeriod();
  if (period) {
    // TODO: bog write another function to map period to start and end labels
    // const { startDate, endDate } = mapPeriodToStartEnd(period);
    // startDateMs = startDate.getTime();
    // endDateMs = endDate.getTime();
    startDateLabel = encodeURIComponent("24 hours ago");
    endDateLabel = "now";

    window.localStorage.setItem("period", "");
  }

  if (!startDateLabel) {
    startDateLabel = encodeURIComponent("24 hours ago");
    setSearchParams((params) => {
      params.set("startDate", encodeURIComponent("24 hours ago"));
      return params;
    });
  }
  if (!endDateLabel) {
    endDateLabel = "now";
    setSearchParams((params) => {
      params.set("endDate", "now");
      return params;
    });
  }

  const suggest = new SuggestionEngine();
  const startDate = suggest
    .generateSuggestions(decodeURIComponent(startDateLabel))
    .find((s) => s.label.toLowerCase() === decodeURIComponent(startDateLabel).toLowerCase()) ?? {
    id: "",
    label: "",
    date: new Date(0),
  };
  const endDate = suggest
    .generateSuggestions(decodeURIComponent(endDateLabel))
    .find((s) => s.label.toLowerCase() === decodeURIComponent(endDateLabel).toLowerCase()) ?? {
    id: "xx",
    label: "now",
    date: new Date(),
  };

  const setStartDate = (date: DateSuggestion) => {
    if (date.label.toLowerCase() === startDate.label.toLowerCase()) {
      return;
    }
    const startDateToStore = encodeURIComponent(date.label);
    setSearchParams((params) => {
      params.set("startDate", startDateToStore);
      return params;
    });
    // setStoredValue(startDateToStore, "startDate");
  };

  const setEndDate = (date: DateSuggestion) => {
    if (date.label.toLowerCase() === endDate.label.toLowerCase()) {
      return;
    }
    const endDateToStore = encodeURIComponent(date.label);
    setSearchParams((params) => {
      params.set("endDate", endDateToStore);
      return params;
    });
    // setStoredValue(endDateToStore, "endDate");
  };

  // const setStartEndDate = ({ startDate, endDate }: { startDate: DateSuggestion; endDate: DateSuggestion }) => {
  //   const startDateToStore = encodeURIComponent(startDate.label);
  //   const endDateToStore = encodeURIComponent(endDate.label);

  //   // setStoredValue(startDateToStore, "startDate");
  //   // setStoredValue(endDateToStore, "endDate");
  //   setSearchParams((params) => {
  //     params.set("startDate", startDateToStore);
  //     params.set("endDate", endDateToStore);
  //     return params;
  //   });
  // };

  const granularity = getGranularity(startDate.date, endDate.date);

  return {
    startDate,
    endDate,
    granularity,
    startDateIso: startDate.date.toISOString(),
    endDateIso: endDate.date.toISOString(),
    setStartDate,
    setEndDate,
  };
}

// export const mapStartEndDateToQueryParams = (startEndDate: StartEndDate) => {
//   return {
//     startDate: startEndDate.startDate.toISOString(),
//     endDate: startEndDate.endDate.toISOString(),
//     granularity: startEndDate.granularity,
//   };
// };

// export type StartEndDate = {
//   startDate: Date;
//   endDate: Date;
//   granularity: Granularity;
// };

function tryParseEndDate(endDateToParse: string | null) {
  if (!endDateToParse) {
    return undefined;
  }
  if (endDateToParse === "now") {
    return endOfMinute(new Date());
  }
  return new Date(endDateToParse);
}

function mapPeriodToStartEnd(period: string): { startDate: Date; endDate: Date } {
  let endDate = endOfDay(new Date());
  let startDate = startOfMinute(sub(endDate, { hours: 24 }));
  // let granularity: Granularity = "hour";

  switch (period) {
    case PERIOD_ENUM["24h"]: {
      endDate = endOfMinute(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 24 }));
      // granularity = "hour";
      break;
    }
    case PERIOD_ENUM["48h"]: {
      endDate = endOfMinute(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 48 }));
      // granularity = "hour";
      break;
    }
    case PERIOD_ENUM.today: {
      endDate = endOfMinute(new Date());
      startDate = startOfDay(new Date());
      // granularity = "hour";
      break;
    }
    case PERIOD_ENUM.yesterday: {
      endDate = sub(endOfDay(new Date()), { days: 1 });
      startDate = sub(startOfDay(new Date()), { days: 1 });
      // granularity = "hour";
      break;
    }
    case PERIOD_ENUM["7d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 7 }));
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM["14d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 14 }));
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM["30d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 30 }));
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM.month: {
      endDate = endOfDay(new Date());
      startDate = startOfMonth(endDate);
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM["last-month"]: {
      endDate = endOfMonth(sub(new Date(), { months: 1 }));
      startDate = startOfMonth(sub(new Date(), { months: 1 }));
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM["90d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 90 }));
      // granularity = "day";
      break;
    }
    case PERIOD_ENUM["180d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 180 }));
      // granularity = "month";
      break;
    }
    case PERIOD_ENUM["365d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfHour(sub(endDate, { days: 365 }));
      // granularity = "month";
      break;
    }
    case PERIOD_ENUM.all: {
      endDate = endOfDay(new Date());
      startDate = new Date(0);
      // granularity = "month";
      break;
    }
  }
  return {
    startDate,
    endDate,
    // granularity,
  };
}

export function getGranularity(startDate: Date | undefined, endDate: Date | undefined) {
  if (!startDate || !endDate) {
    return "hour";
  }
  const diffInHours = differenceInHours(endDate, startDate);
  if (diffInHours < 72) {
    return "hour";
  }

  const diffInDays = differenceInDays(endDate, startDate);
  if (diffInDays > 90) {
    return "month";
  }
  return "day";
}
