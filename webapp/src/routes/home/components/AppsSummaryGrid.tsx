import { Application, AppIcon } from "@app/apps";
import { PageHeading } from "@app/primitives";
import { Link } from "react-router-dom";

type Props = {
  apps: Application[];
};

export function AppsSummaryGrid(props: Props) {
  return (
    <>
      <PageHeading title="Your Apps" subtitle="Select an app to get started" />
      <div className="grid lg:grid-cols-4 gap-6 mt-8">
        {props.apps.map((app) => (
          <Link
            to={`/${app.id}/`}
            key={app.id}
            className="flex items-center space-x-2 border rounded p-2 shadow hover:bg-muted"
          >
            <AppIcon className="w-5 h-5" iconPath={app.iconPath} />
            <span className="truncate">{app.name}</span>
          </Link>
        ))}
      </div>
    </>
  );
}
