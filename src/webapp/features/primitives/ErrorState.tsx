import { IconAlertTriangle } from "@tabler/icons-react";

export function ErrorState() {
  return (
    <div className="w-full h-full text-destructive flex flex-col space-y-1 items-center justify-center">
      <p className="text-lg flex items-center space-x-2">
        <IconAlertTriangle className="h-5 w-5" />
        <span>Oops... Something went wrong.</span>
      </p>
      <p className="text-sm text-muted-foreground">
        Please try again. If the problem persists, please contact support.
      </p>
    </div>
  );
}
