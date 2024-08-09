import { DateSuggestion } from "@datepicker-suggest/core";
import { Granularity } from "@features/analytics/query";
import { getGranularity } from "@hooks/use-datepicker";
import { atom, useAtomValue } from "jotai";
import { atomWithLocation } from "jotai-location";

const locationAtom = atomWithLocation();

function atomWithStorage<T extends string | number | boolean>(key: string, initialValue: T) {
  const defaultValue = localStorage.hasOwnProperty(key) ? decodeURIComponent(localStorage[key]) : initialValue;
  const _local = atom(defaultValue);

  return atom(
    (get) => {
      const searchParamValue = get(locationAtom).searchParams?.get(key);
      if (searchParamValue) {
        return decodeURIComponent(searchParamValue);
      }

      return get(_local);
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

export const startDatePersistentLabelAtom = atomWithStorage<string>("startDateLabel", "24 hours ago");
const startDateBaseAtom = atom<DateSuggestion | null>(null);
export const startDateAtom = atom(
  (get) => get(startDateBaseAtom),
  (get, set, newValue) => {
    const nextValue = typeof newValue === "function" ? newValue(get(startDateBaseAtom)) : newValue;
    set(startDateBaseAtom, nextValue);
    set(startDatePersistentLabelAtom, nextValue?.label ?? "");
  }
);

export const endDatePersistentLabelAtom = atomWithStorage<string>("endDateLabel", "Now");
const endDateBaseAtom = atom<DateSuggestion | null>(null);
export const endDateAtom = atom(
  (get) => get(endDateBaseAtom),
  (get, set, newValue) => {
    const nextValue = typeof newValue === "function" ? newValue(get(endDateBaseAtom)) : newValue;
    set(endDateBaseAtom, nextValue);
    set(endDatePersistentLabelAtom, nextValue?.label ?? "");
  }
);

export const startDateIsoStringAtom = atom((get) => {
  const startDateSuggestion = get(startDateAtom);
  return startDateSuggestion?.date.toISOString();
});

export const endDateIsoStringAtom = atom((get) => {
  const endDateSuggestion = get(endDateAtom);
  return endDateSuggestion?.date.toISOString();
});

export const granularityAtom = atom<Granularity>((get) => {
  const startDateSuggestion = get(startDateAtom);
  const endDateSuggestion = get(endDateAtom);
  return getGranularity(startDateSuggestion?.date, endDateSuggestion?.date);
});

export function readDateSuggestionValues() {
  return {
    startDateIso: useAtomValue(startDateIsoStringAtom),
    endDateIso: useAtomValue(endDateIsoStringAtom),
    granularity: useAtomValue(granularityAtom),
  };
}
