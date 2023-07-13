import { useCurrentApp } from "@app/navigation";
import { Head, PageHeading } from "@app/primitives";
import { DangerZone } from "./components/DangerZone";
import { AppSharing } from "./components/AppSharing";
import { GeneralSettings } from "./components/GeneralSettings";
import * as Tabs from "@radix-ui/react-tabs";

Component.displayName = "Settings";
export function Component() {
  const app = useCurrentApp();

  return (
    <>
      <Head title={`${app.name} - Settings`} />
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <Tabs.Root defaultValue="general" className="">
        <Tabs.List className="inline-flex h-9 items-center text-subtle w-full justify-start rounded-none border-b bg-transparent p-0">
          <Tabs.Trigger value="general">General</Tabs.Trigger>
          <Tabs.Trigger value="sharing">Sharing</Tabs.Trigger>
          <Tabs.Trigger value="danger">Danger Zone</Tabs.Trigger>
        </Tabs.List>
        <Tabs.Content value="general">
          <GeneralSettings />
        </Tabs.Content>
        <Tabs.Content value="sharing">
          <AppSharing />
        </Tabs.Content>
        <Tabs.Content value="danger">
          <DangerZone />
        </Tabs.Content>
      </Tabs.Root>
    </>
  );
}
