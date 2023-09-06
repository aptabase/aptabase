import { IconAlertTriangle } from "@tabler/icons-react";

type Props = {
  reason: string;
};

export function AppLockedContent(props: Props) {
  return (
    <>
      <IconAlertTriangle className="text-warning h-5 w-5" />
      <div>
        <p className="text-center">
          {props.reason === "B" ? "App is locked due to overuse" : "App is locked"}
        </p>
        <p className="text-center text-muted-foreground">
          {props.reason === "B"
            ? "Subscribe to a suitable plan to unlock"
            : "Contact support to unlock"}
        </p>
      </div>
    </>
  );
}
