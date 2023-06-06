import { IconCode, IconLayoutGrid, IconSettings } from "@tabler/icons-react";
import { NavCategory } from "./NavCategory";
import { NavItem } from "./NavItem";
import { isSupportEnabled } from "@app/env";
import { SupportNavCategory } from "@app/support";

export function NavMenu(props: { onNavigation?: VoidFunction }) {
  return (
    <div className="flex flex-col h-full justify-between">
      <div className="space-y-6">
        <NavCategory>
          <NavItem
            label="Dashboard"
            href="/"
            icon={IconLayoutGrid}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Settings"
            href="/settings"
            icon={IconSettings}
            onNavigation={props.onNavigation}
          />
          <NavItem
            label="Instructions"
            href="/instructions"
            icon={IconCode}
            onNavigation={props.onNavigation}
          />
        </NavCategory>
        {isSupportEnabled && <SupportNavCategory />}
      </div>
    </div>
  );
}
