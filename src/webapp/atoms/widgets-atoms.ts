import { produce } from "immer";
import { atom } from "jotai";

export type EventsChartWidgetConfig = {
  selectedEventNames: string[];
};

export type SingleWidgetConfig<T = any> = {
  id: string;
  isMinimized: boolean;
  title: string;
  orderIndex: number;
  isDefined: boolean;
  properties?: T;
  supportsRemove?: boolean;
};

export type WidgetsConfig = SingleWidgetConfig[];
export const DEFAULT_WIDGETS_CONFIG: WidgetsConfig = [
  {
    id: "events-chart",
    title: "Custom Chart",
    isMinimized: false,
    orderIndex: 0,
    isDefined: false,
    properties: {
      selectedEventNames: [],
    },
    supportsRemove: true,
  },
  {
    id: "main-chart",
    title: "Events Chart",
    isMinimized: false,
    orderIndex: 1,
    isDefined: true,
  },
  {
    id: "country",
    title: "Countries",
    isMinimized: false,
    orderIndex: 2,
    isDefined: true,
  },
  {
    id: "os",
    title: "Operating Systems",
    isMinimized: false,
    orderIndex: 3,
    isDefined: true,
  },
  {
    id: "event",
    title: "Events",
    isMinimized: false,
    orderIndex: 4,
    isDefined: true,
  },
  {
    id: "version",
    title: "App Versions",
    isMinimized: false,
    orderIndex: 5,
    isDefined: true,
  },
];

const getDashboardWidgetAtom = atom(
  JSON.parse(localStorage.getItem("dashboard_widgets") ?? JSON.stringify(DEFAULT_WIDGETS_CONFIG))
);

// export const dashboardWidgetsAtom = atom<WidgetsConfig, [SetStateAction<WidgetsConfig>], void>(
//   (get) => get(getDashboardWidgetAtom),
//   (get, set, update) => {
//     const nextValue = typeof update === "function" ? update(get(getDashboardWidgetAtom)) : update;
//     set(getDashboardWidgetAtom, nextValue);
//     localStorage.setItem("dashboard_widgets", JSON.stringify(nextValue || {}));
//   }
// );

export type UpdateDashboardWidgetsAction<T = any> =
  | {
      widgetId: string;
      type: "update-properties";
      properties?: T;
    }
  | {
      widgetId: string;
      type: "update-order";
      active: { widgetId: string; newIndex: number };
      over: { widgetId: string; newIndex: number };
    }
  | {
      widgetId: string;
      type: "toggle-minimized";
    }
  | {
      widgetId: string;
      type: "toggle-is-defined";
    };

export const dashboardWidgetsAtom = atom<WidgetsConfig, [UpdateDashboardWidgetsAction], void>(
  (get) => get(getDashboardWidgetAtom),
  (get, set, action) => {
    const widgetsConfigArray: WidgetsConfig = [...(get(getDashboardWidgetAtom) ?? DEFAULT_WIDGETS_CONFIG)];
    const result = produce(widgetsConfigArray, (draft) => {
      let widgetIndex = draft.findIndex((w) => w.id === action.widgetId);
      if (widgetIndex === -1) {
        draft.push({
          id: action.widgetId,
          title: action.widgetId,
          isMinimized: false,
          orderIndex: draft.length,
          isDefined: true,
        });
        widgetIndex = draft.length - 1;
      }

      if (action.type === "update-properties") {
        draft[widgetIndex].properties = action.properties;
      } else if (action.type === "update-order") {
        const activeWidget = draft.find((w) => w.id === action.active.widgetId);
        const overWidget = draft.find((w) => w.id === action.over.widgetId);
        if (activeWidget) {
          activeWidget.orderIndex = action.active.newIndex;
        }
        if (overWidget) {
          overWidget.orderIndex = action.over.newIndex;
        }
      } else if (action.type === "toggle-minimized") {
        draft[widgetIndex].isMinimized = !draft[widgetIndex].isMinimized;
      } else if (action.type === "toggle-is-defined") {
        draft[widgetIndex].isDefined = !draft[widgetIndex].isDefined;
      }
    });
    set(getDashboardWidgetAtom, result);

    localStorage.setItem("dashboard_widgets", JSON.stringify(result));
  }
);
