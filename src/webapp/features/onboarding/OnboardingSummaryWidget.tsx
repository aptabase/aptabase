import { AppIcon, Application } from "@features/apps";
import { Link } from "react-router-dom";

type Props = {
  app: Application;
};

export function OnboardingSummaryWidget(props: Props) {
  return (
    <Link to={props.app.id} className="border cursor-pointer rounded shadow hover:bg-muted h-full">
      <div className="flex flex-col">
        <div className="p-2 h-16">
          <div className="flex items-start justify-between">
            <div className="flex items-center space-x-2 truncate">
              <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
              <span className="truncate">{props.app.name}</span>
            </div>
          </div>
        </div>
        <div className="flex-1 items-center text-sm space-y-4">
          <p className="text-center">Waiting for the first event...</p>
          <span className="relative flex h-4 w-4 mx-auto items-center justify-center">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-success"></span>
          </span>
        </div>
      </div>
    </Link>
  );
}
