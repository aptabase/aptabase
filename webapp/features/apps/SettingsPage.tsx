import {
  PageHeading,
  Tabs,
  TabsList,
  TabsContent,
  TabsTrigger,
  Page,
} from "@features/primitives";
import { DangerZone } from "./DangerZone";
import { AppSharing } from "./AppSharing";
import { GeneralSettings } from "./GeneralSettings";
import { Navigate } from "react-router-dom";
import { useCurrentApp } from "@features/apps";

Component.displayName = "SettingsPage";
export function Component() {
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  return (
    <Page title={`${app.name} - Settings`}>
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <Tabs defaultValue="general" className="mt-8">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="sharing">Sharing</TabsTrigger>
          <TabsTrigger value="danger">Danger Zone</TabsTrigger>
        </TabsList>
        <TabsContent value="general">
          <GeneralSettings app={app} />
        </TabsContent>
        <TabsContent value="sharing">
          <AppSharing app={app} />
        </TabsContent>
        <TabsContent value="danger">
          <DangerZone app={app} />
        </TabsContent>
      </Tabs>
    </Page>
  );
}
