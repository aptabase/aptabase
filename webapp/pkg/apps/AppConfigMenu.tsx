import { Menu, Transition } from "@headlessui/react";
import { IconBug, IconRocket } from "@tabler/icons-react";
import { Fragment } from "react";
import { BuildModeSelector } from "./BuildModeSelector";
import { useApps } from "./AppsProvider";

export function AppConfigMenu() {
  const { buildMode } = useApps();

  return (
    <Menu as="div" className="relative">
      <Menu.Button className="flex max-w-xs px-2 py-1 items-center rounded text-sm focus-ring hover:bg-accent">
        {buildMode === "release" ? (
          <IconRocket className="h-5 w-5" stroke="1" />
        ) : (
          <IconBug className="h-5 w-5" stroke="1" />
        )}
      </Menu.Button>
      <Transition
        as={Fragment}
        enter="transition ease-out duration-100"
        enterFrom="transform opacity-0 scale-95"
        enterTo="transform opacity-100 scale-100"
        leave="transition ease-in duration-75"
        leaveFrom="transform opacity-100 scale-100"
        leaveTo="transform opacity-0 scale-95"
      >
        <Menu.Items className="absolute right-0 z-10 mt-2 w-60 origin-top-right rounded-md shadow-lg border bg-background focus-ring">
          <Menu.Item>
            <BuildModeSelector />
          </Menu.Item>
        </Menu.Items>
      </Transition>
    </Menu>
  );
}
