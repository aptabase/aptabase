import { Application } from "@features/apps";
import { api } from "@fns/api";
import { IconHelp } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AppShareListItem } from "./AppShareListItem";
import { Button } from "@components/Button";
import { TextInput } from "@components/TextInput";
import { Alert, AlertDescription, AlertTitle } from "@components/Alert";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";

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
  } = useQuery({
    queryKey: ["appshares", props.app.id],
    queryFn: () => api.get<AppShare[]>(`/_apps/${props.app.id}/shares`),
  });

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    await api.put(`/_apps/${props.app.id}/shares/${email.trim()}`);
    setEmail("");
    refetch();
  };

  if (isLoading || !shares) return <LoadingState />;
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
            label="Share with:"
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
        <AlertTitle>How it works?</AlertTitle>
        <AlertDescription className="text-muted-foreground">
          <ol className="list-decimal mx-4 my-2">
            <li>Sharing an app allow other users to have read-only access to your app's dashboards.</li>
            <li>You remain the owner of the app and its billing.</li>
            <li>You can revoke access at any time.</li>
          </ol>

          <p>
            <span className="font-bold">Note:</span> Aptabase won't send an email as part of the sharing process. We
            recommend you getting in contact with them directly and asking them to sign up.
          </p>
        </AlertDescription>
      </Alert>
    </div>
  );
}
