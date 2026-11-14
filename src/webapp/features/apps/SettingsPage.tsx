import { LoadingState } from "@components/LoadingState";
import { Page, PageHeading } from "@components/Page";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@components/Tabs";
import { useCurrentApp } from "@features/apps";
import { Navigate } from "react-router-dom";
import { AppApiUsage } from "./AppApiUsage";
import { AppSharing } from "./AppSharing";
import { DangerZone } from "./DangerZone";
import { GeneralSettings } from "./GeneralSettings";
import { OwnershipTransfer } from "./OwnershipTransfer";
import { useAppApiUsage, useServiceClients } from "./query-api";
import { ServiceClients } from "./ServiceClients";

Component.displayName = "SettingsPage";
export function Component() {
  const app = useCurrentApp();
  const isOwner = !!app?.hasOwnership;

  // Both resolve to null when the query API is switched off on the server
  const usage = useAppApiUsage(app?.id);
  const clients = useServiceClients(app?.id, isOwner);

  if (!app) return <Navigate to="/" />;

  // API usage is the only section a shared user can see. Without it there is
  // nothing to show, so the page behaves as it always did for non-owners.
  if (!isOwner) {
    if (usage.isLoading) return <LoadingState />;
    if (!usage.data) return <Navigate to="/" />;

    return (
      <Page title={`${app.name} - Settings`}>
        <PageHeading title="Settings" subtitle="Other settings are available only to the application owner" />

        <Tabs defaultValue="api-usage" className="mt-8">
          <TabsList>
            <TabsTrigger value="api-usage">API usage</TabsTrigger>
          </TabsList>
          <TabsContent value="api-usage">
            <AppApiUsage app={app} />
          </TabsContent>
        </Tabs>
      </Page>
    );
  }

  return (
    <Page title={`${app.name} - Settings`}>
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <Tabs defaultValue="general" className="mt-8">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="sharing">Sharing</TabsTrigger>
          {clients.data && <TabsTrigger value="service-clients">Service clients</TabsTrigger>}
          {usage.data && <TabsTrigger value="api-usage">API usage</TabsTrigger>}
          <TabsTrigger value="ownership">Ownership Transfer</TabsTrigger>
          <TabsTrigger value="danger">Danger Zone</TabsTrigger>
        </TabsList>
        <TabsContent value="general">
          <GeneralSettings app={app} />
        </TabsContent>
        <TabsContent value="sharing">
          <AppSharing app={app} />
        </TabsContent>
        {clients.data && (
          <TabsContent value="service-clients">
            <ServiceClients app={app} />
          </TabsContent>
        )}
        {usage.data && (
          <TabsContent value="api-usage">
            <AppApiUsage app={app} />
          </TabsContent>
        )}
        <TabsContent value="ownership">
          <OwnershipTransfer app={app} />
        </TabsContent>
        <TabsContent value="danger">
          <DangerZone app={app} />
        </TabsContent>
      </Tabs>
    </Page>
  );
}
