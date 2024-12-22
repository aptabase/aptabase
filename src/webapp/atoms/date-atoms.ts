import { DateSuggestion } from "@datepicker-suggest/core";
import { PERIOD_ENUM } from "@features/analytics/date-filters/DateFilterContainer";
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
import { atom, Getter } from "jotai";
import { locationAtom } from "./location-atoms";

export const periodAtom = atomWithLocalAndUrlPersistence("period", "24h");

// #region startDate
export const startDatePersistentLabelAtom = atomWithLocalAndUrlPersistence<string>("startDateLabel", "24 hours ago");
const startDateBaseAtom = atom<DateSuggestion | null>(null);
export const startDateAtom = atom(
  (get) => get(startDateBaseAtom),
  (get, set, newValue) => {
    const nextValue = typeof newValue === "function" ? newValue(get(startDateBaseAtom)) : newValue;
    set(startDateBaseAtom, nextValue);
    set(startDatePersistentLabelAtom, nextValue?.label ?? "");
  }
);
export const clearStartDatePersistentAtom = atom(null, (get, set) => {
  set(locationAtom, (prev) => {
    prev.searchParams?.delete("startDateLabel");
    return prev;
  });
});
export const startDateIsoStringAtom = atom((get) => {
  const startDateSuggestion = get(startDateAtom);
  return startDateSuggestion?.date.toISOString();
});

// #region endDate
export const endDatePersistentLabelAtom = atomWithLocalAndUrlPersistence<string>("endDateLabel", "Now");
const endDateBaseAtom = atom<DateSuggestion | null>(null);
export const endDateAtom = atom(
  (get) => get(endDateBaseAtom),
  (get, set, newValue) => {
    const nextValue = typeof newValue === "function" ? newValue(get(endDateBaseAtom)) : newValue;
    set(endDateBaseAtom, nextValue);
    set(endDatePersistentLabelAtom, nextValue?.label ?? "");
  }
);
export const clearEndDatePersistentAtom = atom(null, (get, set) => {
  set(locationAtom, (prev) => {
    prev.searchParams?.delete("endDateLabel");
    return prev;
  });
});
export const endDateIsoStringAtom = atom((get) => {
  const endDateSuggestion = get(endDateAtom);
  return endDateSuggestion?.date.toISOString();
});

// #region granularity
export const granularityAtom = atom<Granularity>((get) => {
  const startDateSuggestion = get(startDateAtom);
  const endDateSuggestion = get(endDateAtom);
  return getGranularity(startDateSuggestion?.date, endDateSuggestion?.date);
});

// #region filtersResult
export const dateFilterValuesAtom = atom((get) => {
  const period = get(periodAtom);
  return mapPeriodToStartEnd(period, get);
});

function atomWithLocalAndUrlPersistence<T extends string | number | boolean>(key: string, initialValue: T) {
  const defaultValue = localStorage.hasOwnProperty(key) ? decodeURIComponent(localStorage[key]) : initialValue;
  const _local = atom(defaultValue);

  return atom(
    (get) => {
      const searchParamValue = get(locationAtom).searchParams?.get(key);
      if (searchParamValue) {
        return decodeURIComponent(searchParamValue);
      }

      const localValue = get(_local);
      if (typeof localValue === "string") {
        return decodeURIComponent(localValue);
      }
      return localValue;
    },
    (get, set, newValue: T) => {
      const storedValue = encodeURIComponent(newValue);
      localStorage[key] = storedValue;
      set(locationAtom, (prev) => {
        let newSearchParams = new URLSearchParams([[key, storedValue]]);
        if (prev.searchParams) {
          newSearchParams = prev.searchParams;
        }

        newSearchParams.set(key, storedValue);
        return {
          ...prev,
          searchParams: newSearchParams,
        };
      });
      set(_local, storedValue);
    }
  );
}

function getGranularity(startDate: Date | undefined, endDate: Date | undefined) {
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

function mapPeriodToStartEnd(period: string, getFunc: Getter) {
  let endDate = endOfDay(new Date());
  let startDate = startOfMinute(sub(endDate, { hours: 24 }));
  let granularity: Granularity = "hour";

  if (period === PERIOD_ENUM.custom || !period) {
    return {
      startDateIso: getFunc(startDateIsoStringAtom) ?? "",
      endDateIso: getFunc(endDateIsoStringAtom) ?? "",
      granularity: getFunc(granularityAtom),
    };
  }

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
    startDateIso: startDate.toISOString(),
    endDateIso: endDate.toISOString(),
    granularity,
  };
}
