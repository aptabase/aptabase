import { AppIcon, Application } from "@features/apps";
import { WaitingSignal } from "@features/primitives";
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
          <WaitingSignal size="small" />
          <div>
            <p className="text-center">Waiting for the first event...</p>
            <p className="text-center text-muted-foreground">Click to learn how</p>
          </div>
        </div>
      </div>
    </Link>
  );
}
