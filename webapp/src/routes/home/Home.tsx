import { Page } from "@app/primitives";
import { useApps } from "@app/apps";
import { LonelyState } from "./components/LonelyState";
import { AppsSummaryGrid } from "./components/AppsSummaryGrid";

Component.displayName = "Home";
export function Component() {
  const { apps } = useApps();

  return (
    <Page title="Home">
      {apps.length === 0 ? <LonelyState /> : <AppsSummaryGrid apps={apps} />}
    </Page>
  );
}
