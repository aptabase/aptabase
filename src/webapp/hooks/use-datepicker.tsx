import { PERIOD_ENUM } from "@features/analytics/DateRangePicker";
import {
  endOfDay,
  endOfMonth,
  setMilliseconds,
  setSeconds,
  startOfDay,
  startOfHour,
  startOfMinute,
  startOfMonth,
  sub,
} from "date-fns";
import { useSearchParams } from "react-router-dom";

const setStoredValue = (value: string) => window.localStorage?.setItem("period", value);
const getStoredValue = () => window.localStorage?.getItem("period") ?? "24h";

export function useDatePicker(): {
  startDate: string;
  endDate: string;
  period: string;
  setPeriod: (value: string) => void;
} {
  let [searchParams, setSearchParams] = useSearchParams();
  const period = searchParams.get("period") ?? getStoredValue();
  const startEndDate = mapPeriodToStartEnd(period);

  const setPeriod = (value: string) => {
    setStoredValue(value);
    setSearchParams((params) => {
      params.set("period", value);
      return params;
    });
  };

  return {
    startDate: startEndDate.startDate,
    endDate: startEndDate.endDate,
    period,
    setPeriod,
  };
}

export type StartEndDate = { startDate: string; endDate: string };

function mapPeriodToStartEnd(period: string): StartEndDate {
  let endDate = new Date();
  let startDate = sub(endDate, { days: 1 });

  switch (period) {
    case PERIOD_ENUM["24h"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 24 }));
      break;
    }
    case PERIOD_ENUM["48h"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 48 }));
      break;
    }
    case PERIOD_ENUM.today: {
      endDate = endOfDay(new Date());
      startDate = startOfDay(new Date());
      break;
    }
    case PERIOD_ENUM.yesterday: {
      endDate = sub(endOfDay(new Date()), { days: 1 });
      startDate = sub(startOfDay(new Date()), { days: 1 });
      break;
    }
    case PERIOD_ENUM["7d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 7 }));
      break;
    }
    case PERIOD_ENUM["14d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 14 }));
      break;
    }
    case PERIOD_ENUM["30d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 30 }));
      break;
    }
    case PERIOD_ENUM.month: {
      endDate = endOfDay(new Date());
      startDate = startOfMonth(endDate);
      break;
    }
    case PERIOD_ENUM["last-month"]: {
      endDate = endOfMonth(sub(new Date(), { months: 1 }));
      startDate = startOfMonth(sub(new Date(), { months: 1 }));
      break;
    }
    case PERIOD_ENUM["90d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 90 }));
      break;
    }
    case PERIOD_ENUM["180d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 180 }));
      break;
    }
    case PERIOD_ENUM["365d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfHour(sub(endDate, { days: 365 }));
      break;
    }
    case PERIOD_ENUM.all: {
      endDate = endOfDay(new Date());
      startDate = new Date(0);
      break;
    }
  }
  return { startDate: startDate.toISOString(), endDate: endDate.toISOString() };
}
