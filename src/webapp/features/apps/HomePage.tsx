import { Page } from "@features/primitives";
import { useApps } from "@features/apps";
import { LonelyState } from "./LonelyState";
import { AppsSummaryGrid } from "./AppsSummaryGrid";

Component.displayName = "HomePage";
export function Component() {
  const { apps } = useApps();

  return (
    <Page title="Home">
      {apps.length === 0 ? <LonelyState /> : <AppsSummaryGrid apps={apps} />}
    </Page>
  );
}
