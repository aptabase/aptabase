import { Alert, AlertDescription, AlertTitle } from "@components/Alert";
import { IconFlask } from "@tabler/icons-react";

export function BetaNotice() {
  return (
    <Alert variant="warning" className="my-4">
      <IconFlask className="h-4 w-4" />
      <AlertTitle>Error reporting is in beta</AlertTitle>
      <AlertDescription className="text-muted-foreground">
        <p>
          SDK support is still rolling out. Want it in your SDK?{" "}
          <a target="_blank" className="underline hover:text-foreground" href="https://github.com/aptabase">
            We welcome contributions on GitHub
          </a>
          .
        </p>
      </AlertDescription>
    </Alert>
  );
}
