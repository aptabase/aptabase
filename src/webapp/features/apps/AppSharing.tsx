import { Application } from "@features/apps";
import { ErrorState, LoadingState, api } from "@features/primitives";
import { IconHelp } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AppShareListItem } from "./AppShareListItem";
import { Button } from "@components/Button";
import { TextInput } from "@components/TextInput";
import { Alert, AlertDescription, AlertTitle } from "@components/Alert";

type AppShare = {
  email: string;
  createdAt: string;
};

type Props = {
  app: Application;
};

export function AppSharing(props: Props) {
  const [email, setEmail] = useState("");

  const {
    isLoading,
    isError,
    data: shares,
    refetch,
  } = useQuery(["appshares", props.app.id], () =>
    api.get<AppShare[]>(`/_apps/${props.app.id}/shares`)
  );

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    await api.put(`/_apps/${props.app.id}/shares/${email.trim()}`);
    setEmail("");
    refetch();
  };

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState />;

  return (
    <div className="space-y-8">
      {shares.length >= 1 && (
        <ul className="grid md:grid-cols-3 gap-2">
          {shares.map((s) => (
            <AppShareListItem app={props.app} {...s} onUnshare={refetch} />
          ))}
        </ul>
      )}

      <form onSubmit={handleSubmit} className="space-y-4 max-w-[40rem]">
        <div className="flex items-center space-x-2">
          <TextInput
            label="Share with"
            name="email"
            type="email"
            required={true}
            value={email}
            placeholder="peter.parker@corp.com"
            maxLength={300}
            onChange={(e) => setEmail(e.target.value)}
            description="Enter the email address you want to share your app with."
          />
          <Button disabled={email.length === 0}>Share</Button>
        </div>
      </form>

      <Alert className="max-w-[40rem]">
        <IconHelp className="h-4 w-4" />
        <AlertTitle>What is App Sharing?</AlertTitle>
        <AlertDescription className="text-muted-foreground">
          Sharing an app with other users allow them to have read-only access to your app's
          dashboard.
          <br />
          You remain the owner of the app and can revoke access at any time.
        </AlertDescription>
      </Alert>
    </div>
  );
}
