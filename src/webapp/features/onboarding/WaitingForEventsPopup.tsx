import { Link } from "react-router-dom";
import { isSupportEnabled } from "../env/env";
import { ContactUsLink } from "../support/ContactUsLink";

type Props = {
  appId: string;
};

export function WaitingForEventsPopup(props: Props) {
  return (
    <div className="absolute top-0 left-0 h-full w-full backdrop-blur-sm z-20 flex justify-center items-start lg:items-center">
      <div className="flex flex-col bg-background py-4 w-80 rounded-md border space-y-6 mt-4">
        <p className="text-center font-semibold">Waiting for the first event...</p>
        <p className="text-center text-muted-foreground mx-14">
          Install an{" "}
          <Link to={`/${props.appId}/instructions`} className="text-foreground underline">
            Aptabase SDK
          </Link>{" "}
          on your app to get started.
        </p>
        <span className="relative flex h-6 w-6 mx-auto items-center justify-center">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
          <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-success"></span>
        </span>
        {isSupportEnabled && (
          <div className="w-full space-y-4">
            <hr className="mx-4" />
            <p className="text-xs text-center text-muted-foreground">
              Need help? <ContactUsLink className="text-white">Contact us</ContactUsLink>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
