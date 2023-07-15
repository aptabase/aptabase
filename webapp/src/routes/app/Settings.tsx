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
import { Navigate } from "react-router-dom";
import { useCurrentApp } from "@app/apps";

Component.displayName = "Settings";
export function Component() {
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  return (
    <>
      <Head title={`${app.name} - Settings`} />
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <Tabs defaultValue="general" className="mt-8">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          {/* <TabsTrigger value="sharing">Sharing</TabsTrigger> */}
          <TabsTrigger value="danger">Danger Zone</TabsTrigger>
        </TabsList>
        <TabsContent value="general">
          <GeneralSettings app={app} />
        </TabsContent>
        <TabsContent value="sharing">
          <AppSharing />
        </TabsContent>
        <TabsContent value="danger">
          <DangerZone app={app} />
        </TabsContent>
      </Tabs>
    </>
  );
}
