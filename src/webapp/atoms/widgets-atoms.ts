import { atom, SetStateAction } from "jotai";

export type WidgetConfig = {
  eventsChart?: string[];
};

// export const dashboardWidgetsAtom = atomWithStorage<WidgetConfig>("dashboard_widgets", {
//   eventsChart: [],
// });

const getDashboardWidgetAtom = atom(JSON.parse(localStorage.getItem("dashboard_widgets") ?? "{}"));

export const dashboardWidgetsAtom = atom<WidgetConfig, [SetStateAction<WidgetConfig>], void>(
  (get) => get(getDashboardWidgetAtom),
  (get, set, update) => {
    const nextValue = typeof update === "function" ? update(get(getDashboardWidgetAtom)) : update;
    set(getDashboardWidgetAtom, nextValue);
    localStorage.setItem("dashboard_widgets", JSON.stringify(nextValue || {}));
  }
);
