import { Alert, AlertTitle, AlertDescription } from "@components/Alert";
import { ContactUsLink } from "@features/support";
import { IconSparkles } from "@tabler/icons-react";

export function DevelopmentNotice() {
  return (
    <Alert className="max-w-[38rem] mx-auto">
      <IconSparkles className="h-4 w-4" />
      <AlertTitle>What's next?</AlertTitle>
      <AlertDescription className="text-muted-foreground">
        <p>
          Let us know what would you like to see here. Live View is under active development and we're looking for
          feedback. Reach us by{" "}
          <a href="mailto:hi@aptabase.com" className="underline hover:text-foreground">
            email
          </a>{" "}
          or <ContactUsLink className="underline hover:text-foreground">chat</ContactUsLink>.
        </p>
      </AlertDescription>
    </Alert>
  );
}
