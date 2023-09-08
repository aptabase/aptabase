import { Link } from "react-router-dom";
import { isSupportEnabled } from "@features/env";
import { ContactUsLink } from "@features/support";
import { PingSignal } from "@components/PingSignal";

type Props = {
  appId: string;
};

export function WaitingForEventsContent(props: Props) {
  return (
    <>
      <p className="text-center font-semibold">Waiting for the first event...</p>
      <p className="text-center text-muted-foreground mx-14">
        Install an{" "}
        <Link to={`/${props.appId}/instructions`} className="text-foreground underline">
          Aptabase SDK
        </Link>{" "}
        on your app to get started.
      </p>
      <PingSignal color="success" />
      {isSupportEnabled && (
        <div className="w-full space-y-4">
          <hr className="mx-4" />
          <p className="text-xs text-center text-muted-foreground">
            Need help? <ContactUsLink className="text-white underline">Contact us</ContactUsLink>
          </p>
        </div>
      )}
    </>
  );
}
