import { useApps } from "@app/apps";
import { LonelyState } from "./components/LonelyState";
import { AppsSummaryGrid } from "./components/AppsSummaryGrid";
import { Head } from "@app/primitives";

Component.displayName = "Home";
export function Component() {
  const { apps } = useApps();

  return (
    <>
      <Head title="Home" />
      {apps.length === 0 ? <LonelyState /> : <AppsSummaryGrid apps={apps} />}
    </>
  );
}
