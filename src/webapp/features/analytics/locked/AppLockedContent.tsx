import { IconAlertTriangle } from "@tabler/icons-react";

type Props = {
  reason: string;
};

const titles: Record<string, string> = {
  B: "App is locked due to billing",
  T: "Expired Trial",
};

const desriptions: Record<string, string> = {
  B: "Subscribe to a suitable plan to unlock",
  T: "Subscribe to unlock",
};

export function AppLockedContent(props: Props) {
  const title = titles[props.reason] || "App is locked";
  const description = desriptions[props.reason] || "Contact support to unlock";

  return (
    <>
      <IconAlertTriangle className="text-warning h-5 w-5" />
      <div>
        <p className="text-center">{title}</p>
        <p className="text-center text-muted-foreground">{description}</p>
      </div>
    </>
  );
}
