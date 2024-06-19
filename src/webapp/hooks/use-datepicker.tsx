import { PERIOD_ENUM } from "@features/analytics/DateRangePicker";
import { Granularity } from "@features/analytics/query";
import { endOfDay, endOfMinute, endOfMonth, startOfDay, startOfHour, startOfMinute, startOfMonth, sub } from "date-fns";
import { useSearchParams } from "react-router-dom";

const setStoredValue = (value: string) => window.localStorage?.setItem("period", value);
const getStoredValue = () => window.localStorage?.getItem("period") ?? "24h";

export function useDatePicker(): {
  startDate: string;
  endDate: string;
  granularity: Granularity;
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
    granularity: startEndDate.granularity,
    period,
    setPeriod,
  };
}

export type StartEndDate = {
  startDate: string;
  endDate: string;
  granularity: Granularity;
};

function mapPeriodToStartEnd(period: string): StartEndDate {
  let endDate = endOfDay(new Date());
  let startDate = startOfMinute(sub(endDate, { hours: 24 }));
  let granularity: Granularity = "hour";

  switch (period) {
    case PERIOD_ENUM["24h"]: {
      endDate = endOfMinute(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 24 }));
      granularity = "hour";
      break;
    }
    case PERIOD_ENUM["48h"]: {
      endDate = endOfMinute(new Date());
      startDate = startOfMinute(sub(endDate, { hours: 48 }));
      granularity = "hour";
      break;
    }
    case PERIOD_ENUM.today: {
      endDate = endOfMinute(new Date());
      startDate = startOfDay(new Date());
      granularity = "hour";
      break;
    }
    case PERIOD_ENUM.yesterday: {
      endDate = sub(endOfDay(new Date()), { days: 1 });
      startDate = sub(startOfDay(new Date()), { days: 1 });
      granularity = "hour";
      break;
    }
    case PERIOD_ENUM["7d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 7 }));
      granularity = "day";
      break;
    }
    case PERIOD_ENUM["14d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 14 }));
      granularity = "day";
      break;
    }
    case PERIOD_ENUM["30d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 30 }));
      granularity = "day";
      break;
    }
    case PERIOD_ENUM.month: {
      endDate = endOfDay(new Date());
      startDate = startOfMonth(endDate);
      granularity = "day";
      break;
    }
    case PERIOD_ENUM["last-month"]: {
      endDate = endOfMonth(sub(new Date(), { months: 1 }));
      startDate = startOfMonth(sub(new Date(), { months: 1 }));
      granularity = "day";
      break;
    }
    case PERIOD_ENUM["90d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 90 }));
      granularity = "day";
      break;
    }
    case PERIOD_ENUM["180d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfMinute(sub(endDate, { days: 180 }));
      granularity = "month";
      break;
    }
    case PERIOD_ENUM["365d"]: {
      endDate = endOfDay(new Date());
      startDate = startOfHour(sub(endDate, { days: 365 }));
      granularity = "month";
      break;
    }
    case PERIOD_ENUM.all: {
      endDate = endOfDay(new Date());
      startDate = new Date(0);
      granularity = "month";
      break;
    }
  }
  return {
    startDate: startDate.toISOString(),
    endDate: endDate.toISOString(),
    granularity,
  };
}
