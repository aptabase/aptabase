import { produce } from "immer";
import { atom } from "jotai";

export type EventsChartWidgetConfig = {
  selectedEventNames: string[];
};

type WidgetType =
  | "custom-events-chart"
  | "events-chart"
  | "countries"
  | "operating-systems"
  | "events"
  | "app-versions";
const userDefinedWidgetTypes: WidgetType[] = ["custom-events-chart"];

export type SingleWidgetConfig<T = any> = {
  id: string;
  title: string;
  type: WidgetType;
  isDefined: boolean;
  isMinimized: boolean;
  orderIndex: number;
  properties?: T;
  supportsRemove?: boolean;
};

export type WidgetsConfig = SingleWidgetConfig[];
export const DEFAULT_WIDGETS_CONFIG: WidgetsConfig = [
  {
    id: "events-chart",
    title: "Custom Chart",
    type: "custom-events-chart",
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
    type: "events-chart",
    isMinimized: false,
    orderIndex: 1,
    isDefined: true,
  },
  {
    id: "country",
    title: "Countries",
    type: "countries",
    isMinimized: false,
    orderIndex: 2,
    isDefined: true,
  },
  {
    id: "os",
    title: "Operating Systems",
    type: "operating-systems",
    isMinimized: false,
    orderIndex: 3,
    isDefined: true,
  },
  {
    id: "event",
    title: "Events",
    type: "events",
    isMinimized: false,
    orderIndex: 4,
    isDefined: true,
  },
  {
    id: "version",
    title: "App Versions",
    type: "app-versions",
    isMinimized: false,
    orderIndex: 5,
    isDefined: true,
  },
];

const EMPTY_CUSTOM_EVENTS_CHART_WIDGET: SingleWidgetConfig<EventsChartWidgetConfig> = {
  id: "events-chart",
  title: "Custom Chart",
  type: "custom-events-chart",
  isMinimized: false,
  orderIndex: 0,
  isDefined: false,
  properties: {
    selectedEventNames: [],
  },
  supportsRemove: true,
};

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
          type: "custom-events-chart",
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

        if (draft[widgetIndex].isDefined) {
          const areAllUserDefinedWidgetsDefined = draft
            .filter((w) => userDefinedWidgetTypes.includes(w.type))
            .every((w) => w.isDefined);
          if (areAllUserDefinedWidgetsDefined) {
            draft.push(
              produce(EMPTY_CUSTOM_EVENTS_CHART_WIDGET, (customEventsDraft) => {
                customEventsDraft.id = `${customEventsDraft.id}-${widgetsConfigArray.length}`;
                customEventsDraft.orderIndex = widgetsConfigArray.length;
              })
            );
          }
        } else {
          const widgetsToRemoveIds = draft
            .filter((w) => userDefinedWidgetTypes.includes(w.type) && !w.isDefined && w.id !== action.widgetId)
            .map((w) => w.id);
          const widgetsToRemoveIndexes = widgetsToRemoveIds.map((id) => draft.findIndex((w) => w.id === id));
          widgetsToRemoveIndexes.forEach((toRemoveIndex) => {
            draft.splice(toRemoveIndex, 1);
          });
        }
      }
    });
    set(getDashboardWidgetAtom, result);

    localStorage.setItem("dashboard_widgets", JSON.stringify(result));
  }
);
