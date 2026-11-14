import { useCurrentApp } from "@features/apps";
import { useAppApiUsage } from "@features/apps/query-api";
import { isSupportEnabled } from "@features/env";
import { SupportNavCategory } from "@features/support";
import {
  IconActivityHeartbeat,
  IconAlertTriangle,
  IconCloudDownload,
  IconCode,
  IconGraph,
  IconLayoutGrid,
  IconSettings,
  IconUsers,
} from "@tabler/icons-react";
import { useLocation } from "react-router-dom";
import { NavCategory } from "./NavCategory";
import { NavItem } from "./NavItem";

export function NavMenu(props: { onNavigation?: VoidFunction }) {
  const currentApp = useCurrentApp();
  const location = useLocation();

  // A shared user can open the settings page only for its API usage section,
  // which exists when the query API is switched on. Owners need no lookup.
  const isSharedApp = !!currentApp && !currentApp.hasOwnership;
  const { data: apiUsage } = useAppApiUsage(currentApp?.id, isSharedApp);
  const canOpenSettings = !!currentApp && (currentApp.hasOwnership || !!apiUsage);

  return (
    <div className="flex flex-col h-full justify-between">
      <div className="space-y-6">
        <NavCategory>
          <NavItem label="Home" href="/" icon={IconLayoutGrid} onNavigation={props.onNavigation} />
        </NavCategory>
        <NavCategory title="Application">
          <NavItem
            label="Dashboard"
            disabled={!currentApp}
            href={`/${currentApp?.id}/`}
            icon={IconGraph}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Live View"
            disabled={!currentApp || !!currentApp.lockReason}
            href={`/${currentApp?.id}/live`}
            icon={IconActivityHeartbeat}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="User Sessions"
            disabled={!currentApp || !!currentApp.lockReason}
            href={`/${currentApp?.id}/sessions${location.search}`}
            icon={IconUsers}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Errors"
            disabled={!currentApp || !!currentApp.lockReason}
            href={`/${currentApp?.id}/errors`}
            icon={IconAlertTriangle}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Export"
            disabled={!currentApp || !!currentApp.lockReason}
            href={`/${currentApp?.id}/export`}
            icon={IconCloudDownload}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Instructions"
            disabled={!currentApp}
            href={`/${currentApp?.id}/instructions`}
            icon={IconCode}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Settings"
            disabled={!canOpenSettings}
            disabledReason={isSharedApp ? "Settings are available only to application owners" : undefined}
            href={`/${currentApp?.id}/settings`}
            icon={IconSettings}
            onNavigation={props.onNavigation}
          />
        </NavCategory>
        {isSupportEnabled && <SupportNavCategory />}
      </div>
    </div>
  );
}
