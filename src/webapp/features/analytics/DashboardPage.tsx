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
import { rectSortingStrategy, SortableContext } from "@dnd-kit/sortable";
import { useApps, useCurrentApp } from "@features/apps";
import { useAtomValue, useSetAtom } from "jotai/react";
import { useMemo } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { dashboardWidgetsAtom, getDashboardWidgetsForAppAtom, SingleWidgetConfig } from "../../atoms/widgets-atoms";
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
  const sensors = useSensors(useSensor(MouseSensor), useSensor(TouchSensor));

  if (!app) return <Navigate to="/" />;
  if (app.lockReason) {
    return (
      <TeaserDashboardContainer app={app}>
        <AppLockedContent reason={app.lockReason} />
      </TeaserDashboardContainer>
    );
  }

  if (!app.hasEvents) return <OnboardingDashboard app={app} />;

  const getWidgetsForApp = useAtomValue(getDashboardWidgetsForAppAtom);
  const widgetsConfig = getWidgetsForApp(app.id);
  const setWidgetsConfig = useSetAtom(dashboardWidgetsAtom);
  const widgetsOrder = useMemo(
    () => widgetsConfig.toSorted((wa, wb) => wa.orderIndex - wb.orderIndex).map((w) => w.id),
    [widgetsConfig]
  );

  const resetFilters = () => navigate(`/${app.id}/`);

  const toggleMinimize = (widgetId: string) => {
    setWidgetsConfig({
      type: "toggle-minimized",
      widgetId,
      appId: app.id,
    });
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const newOverIndex = widgetsOrder.indexOf(active.id.toString());
      const newActiveIndex = widgetsOrder.indexOf(over.id.toString());

      setWidgetsConfig({
        type: "update-order",
        widgetId: active.id.toString(),
        active: { widgetId: active.id.toString(), newIndex: newActiveIndex },
        over: { widgetId: over.id.toString(), newIndex: newOverIndex },
        appId: app.id,
      });
    }
  };

  const removeWidget = (widgetId: string) => {
    setWidgetsConfig({
      type: "toggle-is-defined",
      widgetId,
      appId: app.id,
    });
  };

  const renderWidget = (widgetId: string) => {
    const props = { appId: app.id, appName: app.name };
    const widget = widgetsConfig.find((w: SingleWidgetConfig) => w.id === widgetId)!;

    switch (widget.type) {
      case "custom-events-chart":
        return (
          <WidgetContainer
            key={widgetId}
            widgetConfig={widget}
            widgetName={widget?.title ?? "Custom Chart"}
            className="col-span-2"
            onToggleMinimize={() => toggleMinimize(widgetId)}
            onRemove={() => removeWidget(widgetId)}
          >
            <EventsChartWidget {...props} widgetConfig={widget} />
          </WidgetContainer>
        );
      case "events-chart":
        return (
          <WidgetContainer
            key={widgetId}
            widgetConfig={widget}
            widgetName={widget?.title ?? "Events Chart"}
            className="col-span-2"
            onToggleMinimize={() => toggleMinimize(widgetId)}
          >
            <MainChartWidget {...props} />
          </WidgetContainer>
        );
      case "countries":
        return (
          <LazyLoad key={widgetId}>
            <WidgetContainer
              widgetConfig={widget}
              widgetName={widget?.title ?? "Countries"}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <CountryWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "operating-systems":
        return (
          <LazyLoad key={widgetId}>
            <WidgetContainer
              widgetConfig={widget}
              widgetName={widget?.title ?? "Operating Systems"}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <OSWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "events":
        return (
          <LazyLoad key={widgetId}>
            <WidgetContainer
              widgetConfig={widget}
              widgetName={widget?.title ?? "Events"}
              onToggleMinimize={() => toggleMinimize(widgetId)}
              className="h-full"
            >
              <EventWidget {...props} />
            </WidgetContainer>
          </LazyLoad>
        );
      case "app-versions":
        return (
          <LazyLoad key={widgetId}>
            <WidgetContainer
              widgetConfig={widget}
              widgetName={widget?.title ?? "App Versions"}
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
          <SortableContext items={widgetsOrder} strategy={rectSortingStrategy}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {widgetsOrder.map((widgetId: string) => renderWidget(widgetId))}
            </div>
          </SortableContext>
        </DndContext>
      </div>
    </Page>
  );
}
