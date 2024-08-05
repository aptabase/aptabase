import { PERIOD_ENUM } from "@features/analytics/DateRangePicker";
import { Granularity } from "@features/analytics/query";
import {
  differenceInDays,
  differenceInHours,
  differenceInMinutes,
  endOfDay,
  endOfMinute,
  endOfMonth,
  startOfDay,
  startOfHour,
  startOfMinute,
  startOfMonth,
  sub,
  subHours,
} from "date-fns";
import { useSearchParams } from "react-router-dom";

const setStoredValue = (value: string, key: string) => window.localStorage.setItem(`aptabase-${key}`, value);
const getStoredValue = (key: string) => {
  const dateStr = window.localStorage.getItem(`aptabase-${key}`);
  return dateStr ? dateStr : null;
};
const getStoredPeriod = () => window.localStorage.getItem("period");

export function useDatePicker(): {
  startDate: Date;
  endDate: Date;
  granularity: Granularity;
  setStartEndDate: ({ startDate, endDate }: { startDate: Date; endDate: Date }) => void;
} {
  let [searchParams, setSearchParams] = useSearchParams();
  let startDateMs = +(searchParams.get("startDate") ?? getStoredValue("startDate") ?? "");
  const endDatePersisted = searchParams.get("endDate") ?? getStoredValue("endDate");

  let endDateMs = tryParseEndDate(endDatePersisted)?.getTime();

  const period = searchParams.get("period") ?? getStoredPeriod();
  if (period) {
    const { startDate, endDate } = mapPeriodToStartEnd(period);
    startDateMs = startDate.getTime();
    endDateMs = endDate.getTime();

    window.localStorage.setItem("period", "");
  }

  if (!startDateMs && !endDateMs) {
    const { startDate, endDate } = mapPeriodToStartEnd(PERIOD_ENUM["24h"]);
    startDateMs = startDate.getTime();
    endDateMs = endDate.getTime();
  }

  if (!startDateMs) {
    startDateMs = subHours(new Date(), 24).getTime();
  }
  if (!endDateMs) {
    endDateMs = new Date().getTime();
  }

  const setStartEndDate = ({ startDate, endDate }: { startDate: Date; endDate: Date }) => {
    let endDateToStore = endDate.getTime().toString();
    if (differenceInMinutes(new Date(), endDate) < 1) {
      endDateToStore = "now";
    }

    const startDateToStore = startDate.getTime().toString();

    setStoredValue(startDateToStore, "startDate");
    setStoredValue(endDateToStore, "endDate");
    setSearchParams((params) => {
      params.set("startDate", startDateToStore);
      params.set("endDate", endDateToStore);
      return params;
    });
  };

  // const [startEndDate, setStartEndDate] = useState<StartEndDate>(mapPeriodToStartEnd(period));

  // const storedStartDate = getStoredValue("startDate");
  // const storedEndDate = getStoredValue("endDate");
  // if (storedStartDate) {
  //   startEndDate.startDate = new Date(storedStartDate);
  // }
  // if (storedEndDate) {
  //   startEndDate.endDate = new Date(storedEndDate);
  // }

  // useEffect(() => {
  //   setDatePeriod({ startDate: startEndDate.startDate, endDate: startEndDate.endDate });
  // }, [startEndDate]);

  // const setDatePeriod = (value: { startDate?: Date; endDate?: Date }) => {
  //   if (value.startDate) {
  //     setStoredValue(value.startDate, "startDate");
  //     // setSearchParams((params) => {
  //     //   params.set("startDate", value.startDate!.toISOString());
  //     //   return params;
  //     // });
  //   }
  //   if (value.endDate) {
  //     setStoredValue(value.endDate, "endDate");
  //     // setSearchParams((params) => {
  //     //   params.set("endDate", value.endDate!.toISOString());
  //     //   return params;
  //     // });
  //   }

  /*if (value.startDate?.getTime() != startEndDate.startDate.getTime()) {
      setStartEndDate({
        startDate: value.startDate ?? startEndDate.startDate,
        endDate: { ...startEndDate.endDate },
        granularity: startEndDate.granularity,
      });
    }

    if (value.endDate?.getTime() != startEndDate.endDate.getTime()) {
      setStartEndDate({
        startDate: { ...startEndDate.startDate },
        endDate: value.endDate ?? startEndDate.endDate,
        granularity: startEndDate.granularity,
      });
    }*/
  // };

  const startDate = new Date(startDateMs);
  const endDate = new Date(endDateMs);
  const granularity = getGranularity(startDate, endDate);

  return {
    startDate,
    endDate,
    granularity,
    setStartEndDate,
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

function getGranularity(startDate: Date, endDate: Date) {
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
