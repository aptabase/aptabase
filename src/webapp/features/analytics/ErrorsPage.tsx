import { Page, PageHeading } from "@components/Page";
import { useCurrentApp } from "@features/apps";
import { Navigate } from "react-router-dom";
import { ErrorsList } from "./errors/ErrorsList";

Component.displayName = "ErrorsPage";
export function Component() {
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  return (
    <Page title="Error Logs">
      <div className="flex justify-between items-center">
        <PageHeading
          title="Errors"
          subtitle="Track and debug crashes and errors in your app"
        />
      </div>
      <ErrorsList appId={app.id} />
    </Page>
  );
}
