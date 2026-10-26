import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";

let currentApp: unknown;

vi.mock("@features/apps", () => ({
  useApps: () => ({ buildMode: "release" }),
  useCurrentApp: () => currentApp,
}));
vi.mock("../../atoms/widgets-atoms", () => ({
  dashboardWidgetsAtom: {},
  getDashboardWidgetsForAppAtom: {},
}));
vi.mock("jotai/react", () => ({
  useAtomValue: () => () => [],
  useSetAtom: () => () => {},
}));
vi.mock("@dnd-kit/core", () => ({
  closestCenter: {},
  DndContext: ({ children }: { children: unknown }) => <>{children}</>,
  MouseSensor: {},
  TouchSensor: {},
  useSensor: () => ({}),
  useSensors: () => [],
}));
vi.mock("@dnd-kit/sortable", () => ({
  rectSortingStrategy: {},
  SortableContext: ({ children }: { children: unknown }) => <>{children}</>,
}));
vi.mock("@components/Page", () => ({
  Page: ({ children }: { children: unknown }) => <>{children}</>,
  PageHeading: () => null,
}));
vi.mock("@components/LazyLoad", () => ({ LazyLoad: () => null }));
vi.mock("@components/Tooltip", () => ({
  Tooltip: () => null,
  TooltipContent: () => null,
  TooltipProvider: () => null,
  TooltipTrigger: () => null,
}));
vi.mock("./CurrentFilters", () => ({ CurrentFilters: () => null }));
vi.mock("./dashboard/AppShareInfo", () => ({ AppShareInfo: () => null }));
vi.mock("./dashboard/CountryWidget", () => ({ CountryWidget: () => null }));
vi.mock("./dashboard/EventWidget", () => ({ EventWidget: () => null }));
vi.mock("./dashboard/OSWidget", () => ({ OSWidget: () => null }));
vi.mock("./dashboard/OnboardingDashboard", () => ({ OnboardingDashboard: () => <p>onboarding</p> }));
vi.mock("./dashboard/TeaserDashboardContainer", () => ({ TeaserDashboardContainer: () => null }));
vi.mock("./dashboard/VersionWidget", () => ({ VersionWidget: () => null }));
vi.mock("./dashboard/WidgetContainer", () => ({ WidgetContainer: () => null }));
vi.mock("./dashboard/custom-widgets/EventsChartWidget", () => ({ EventsChartWidget: () => null }));
vi.mock("./date-filters/DateFilterContainer", () => ({ DateFilterContainer: () => null }));
vi.mock("./key_metrics/MainChartWidget", () => ({ MainChartWidget: () => null }));
vi.mock("./locked/AppLockedContent", () => ({ AppLockedContent: () => null }));
vi.mock("./mode/BuildModeSelector", () => ({ BuildModeSelector: () => null }));
vi.mock("./mode/DebugModeBanner", () => ({ DebugModeBanner: () => null }));

import { Component as DashboardPage } from "./DashboardPage";

const app = (hasEvents: boolean) => ({
  id: "a1",
  name: "App",
  appKey: "A-1",
  iconPath: "",
  hasEvents,
  hasOwnership: true,
});

describe("DashboardPage", () => {
  // Regression: hooks used to be called after the early returns, so switching
  // between an app with events and one without crashed with React error #300.
  it.each([
    ["with events to without", true, false],
    ["without events to with", false, true],
  ])("survives switching from an app %s", (_name, from, to) => {
    const errors = vi.spyOn(console, "error").mockImplementation(() => {});
    currentApp = app(from);
    const view = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    );

    currentApp = app(to);
    expect(() =>
      view.rerender(
        <MemoryRouter>
          <DashboardPage />
        </MemoryRouter>
      )
    ).not.toThrow();
    errors.mockRestore();
  });
});
