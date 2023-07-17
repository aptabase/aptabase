import {
  IconCode,
  IconGraph,
  IconHome,
  IconSettings,
} from "@tabler/icons-react";
import { NavCategory } from "./NavCategory";
import { NavItem } from "./NavItem";
import { isSupportEnabled } from "@app/env";
import { SupportNavCategory } from "@app/support";
import { useCurrentApp } from "../apps/AppsProvider";

export function NavMenu(props: { onNavigation?: VoidFunction }) {
  const currentApp = useCurrentApp();

  return (
    <div className="flex flex-col h-full justify-between">
      <div className="space-y-6">
        <NavCategory>
          <NavItem
            label="Home"
            href="/"
            icon={IconHome}
            onNavigation={props.onNavigation}
          />
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
