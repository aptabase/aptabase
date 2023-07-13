import { useCurrentApp } from "@app/navigation";
import {
  Head,
  PageHeading,
  Tabs,
  TabsList,
  TabsContent,
  TabsTrigger,
} from "@app/primitives";
import { DangerZone } from "./components/DangerZone";
import { AppSharing } from "./components/AppSharing";
import { GeneralSettings } from "./components/GeneralSettings";

Component.displayName = "Settings";
export function Component() {
  const app = useCurrentApp();

  return (
    <>
      <Head title={`${app.name} - Settings`} />
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <Tabs defaultValue="general" className="mt-8">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="sharing">Sharing</TabsTrigger>
          <TabsTrigger value="danger">Danger Zone</TabsTrigger>
        </TabsList>
        <TabsContent value="general">
          <GeneralSettings />
        </TabsContent>
        <TabsContent value="sharing">
          <AppSharing />
        </TabsContent>
        <TabsContent value="danger">
          <DangerZone />
        </TabsContent>
      </Tabs>
    </>
  );
}
