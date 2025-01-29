import { atom } from "jotai";
import { atomWithLocation } from "jotai-location";

export const locationAtom = atomWithLocation();

export function atomWithSearchParam(eventName: string) {
  return atom(
    (get) => get(locationAtom).searchParams?.get(eventName) ?? "",
    (get, set, name) => {
      const nextSearchParams = get(locationAtom).searchParams ?? new URLSearchParams();
      if (name) {
        nextSearchParams.set(eventName, name as string);
      } else {
        nextSearchParams.delete(eventName);
      }
      set(locationAtom, (prev) => ({
        ...prev,
        searchParams: nextSearchParams,
      }));
    }
  );
}
