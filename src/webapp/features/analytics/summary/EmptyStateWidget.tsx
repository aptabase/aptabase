import { AppIcon, Application } from "@features/apps";
import { Link } from "react-router-dom";

type Props = {
  app: Application;
  children: React.ReactNode;
};

export function EmptyStateWidget(props: Props) {
  return (
    <Link
      to={`${props.app.id}/`}
      className="border dark:border-none cursor-pointer rounded shadow-md bg-card hover:bg-muted h-full"
    >
      <div className="flex flex-col">
        <div className="p-2 h-12">
          <div className="flex items-start justify-between">
            <div className="flex items-center space-x-2 truncate">
              <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
              <span className="truncate">{props.app.name}</span>
            </div>
          </div>
        </div>
        <div className="flex flex-col flex-1 items-center space-y-4 text-sm">{props.children}</div>
      </div>
    </Link>
  );
}
