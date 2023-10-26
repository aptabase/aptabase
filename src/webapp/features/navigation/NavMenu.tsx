import {
  IconCode,
  IconGraph,
  IconLayoutGrid,
  IconSettings,
  IconActivityHeartbeat,
  IconCloudDownload,
} from "@tabler/icons-react";
import { NavCategory } from "./NavCategory";
import { NavItem } from "./NavItem";
import { isSupportEnabled } from "@features/env";
import { SupportNavCategory } from "@features/support";
import { useCurrentApp } from "@features/apps";

export function NavMenu(props: { onNavigation?: VoidFunction }) {
  const currentApp = useCurrentApp();

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
            disabled={!currentApp}
            href={`/${currentApp?.id}/live`}
            icon={IconActivityHeartbeat}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Export"
            disabled={!currentApp}
            href={`/${currentApp?.id}/export`}
            icon={IconCloudDownload}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Settings"
            disabled={!currentApp || !currentApp.hasOwnership}
            href={`/${currentApp?.id}/settings`}
            icon={IconSettings}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Instructions"
            disabled={!currentApp || !currentApp.hasOwnership}
            href={`/${currentApp?.id}/instructions`}
            icon={IconCode}
            onNavigation={props.onNavigation}
          />
        </NavCategory>
        {isSupportEnabled && <SupportNavCategory />}
      </div>
    </div>
  );
}
