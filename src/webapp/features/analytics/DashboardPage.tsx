import { LazyLoad } from "@components/LazyLoad";
import { Page, PageHeading } from "@components/Page";
import {
  closestCenter,
  DndContext,
  DragEndEvent,
  MouseSensor,
  TouchSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import { arraySwap, rectSortingStrategy, SortableContext } from "@dnd-kit/sortable";
import { useApps, useCurrentApp } from "@features/apps";
import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { CurrentFilters } from "./CurrentFilters";
import { CountryWidget } from "./dashboard/CountryWidget";
import { EventWidget } from "./dashboard/EventWidget";
import { OSWidget } from "./dashboard/OSWidget";
import { OnboardingDashboard } from "./dashboard/OnboardingDashboard";
import { TeaserDashboardContainer } from "./dashboard/TeaserDashboardContainer";
import { VersionWidget } from "./dashboard/VersionWidget";
import { WidgetContainer } from "./dashboard/WidgetContainer";
import { EventsChartWidget } from "./dashboard/custom-widgets/EventsChartWidget";
import { DateFilterContainer } from "./date-filters/DateFilterContainer";
import { MainChartWidget } from "./key_metrics/MainChartWidget";
import { AppLockedContent } from "./locked/AppLockedContent";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";

Component.displayName = "DashboardPage";

export function Component() {
  const { buildMode } = useApps();
  const app = useCurrentApp();
  const navigate = useNavigate();
  const [minimizedWidgets, setMinimizedWidgets] = useState<Record<string, boolean>>({});
  const [widgetOrder, setWidgetOrder] = useState(["events-chart", "main-chart", "country", "os", "event", "version"]);
  const sensors = useSensors(useSensor(MouseSensor), useSensor(TouchSensor));

  const toggleMinimize = (widgetId: string) => {
    setMinimizedWidgets((prev) => ({
      ...prev,
      [widgetId]: !prev[widgetId],
    }));
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      setWidgetOrder((items) => {
        const oldIndex = items.indexOf(active.id.toString());
        const newIndex = items.indexOf(over.id.toString());
        return arraySwap(items, oldIndex, newIndex);
      });
    }
  };

  if (!app) return <Navigate to="/" />;
  if (app.lockReason) {
    return (
      <TeaserDashboardContainer app={app}>
        <AppLockedContent reason={app.lockReason} />
      </TeaserDashboardContainer>
    );
  }

  if (!app.hasEvents) return <OnboardingDashboard app={app} />;

  const resetFilters = () => navigate(`/${app.id}/`);

  const renderWidget = (widgetId: string) => {
    const props = { appId: app.id, appName: app.name };

    switch (widgetId) {
      case "events-chart":
        return (
          <WidgetContainer
            id={widgetId}
            widgetName="Custom Chart"
            className="col-span-2"
            isMinimized={minimizedWidgets[widgetId]}
            onToggleMinimize={() => toggleMinimize(widgetId)}
          >
            <EventsChartWidget {...props} />
          </WidgetContainer>
        );
      case "main-chart":
        return (
          <WidgetContainer
            id={widgetId}
            widgetName="Events Chart"
            className="col-span-2"
            isMinimized={minimizedWidgets[widgetId]}
            onToggleMinimize={() => toggleMinimize(widgetId)}
          >
            <MainChartWidget {...props} />
          </WidgetContainer>
        );
      case "country":
        return (
          <LazyLoad>
            <WidgetContainer
              id={widgetId}
              widgetName="Countries"
              isMinimized={minimizedWidgets[widgetId]}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <CountryWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "os":
        return (
          <LazyLoad>
            <WidgetContainer
              id={widgetId}
              widgetName="Operating Systems"
              isMinimized={minimizedWidgets[widgetId]}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <OSWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "event":
        return (
          <LazyLoad>
            <WidgetContainer
              id={widgetId}
              widgetName="Events"
              isMinimized={minimizedWidgets[widgetId]}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <EventWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "version":
        return (
          <LazyLoad>
            <WidgetContainer
              id={widgetId}
              widgetName="App Versions"
              isMinimized={minimizedWidgets[widgetId]}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <VersionWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      default:
        return null;
    }
  };

  return (
    <Page title={app.name}>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <PageHeading title="Dashboard" onClick={resetFilters} />
          <div className="flex items-end space-x-2">
            <BuildModeSelector />
            <DateFilterContainer />
          </div>
        </div>
        <div className="flex w-full justify-end">
          <CurrentFilters />
        </div>
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={widgetOrder} strategy={rectSortingStrategy}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {widgetOrder.map((widgetId) => renderWidget(widgetId))}
            </div>
          </SortableContext>
        </DndContext>
      </div>
    </Page>
  );
}
