import { Alert, AlertDescription, AlertTitle } from "@components/Alert";
import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { TextInput } from "@components/TextInput";
import { Application } from "@features/apps";
import { api } from "@fns/api";
import { IconHelp, IconShare } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { AppShareListItem } from "./AppShareListItem";

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
    isLoading: sharesLoading,
    isError: sharesError,
    data: shares,
    refetch: refetchShares,
  } = useQuery({
    queryKey: ["appshares", props.app.id],
    queryFn: () => api.get<AppShare[]>(`/_apps/${props.app.id}/shares`),
  });

  const handleShareSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    await api.put(`/_apps/${props.app.id}/shares/${email.trim()}`);
    setEmail("");
    refetchShares();
  };

  if (sharesLoading) return <LoadingState />;
  if (sharesError) return <ErrorState />;

  return (
    <div className="space-y-8">
      {/* Current Shares Section */}
      <div>
        <div className="flex items-center space-x-2 mb-4">
          <IconShare className="h-5 w-5 text-foreground" />
          <h3 className="text-lg font-medium">Shared With</h3>
        </div>
        {shares && shares.length >= 1 && (
          <ul className="grid md:grid-cols-3 gap-2 mb-6">
            {shares.map((s) => (
              <AppShareListItem key={s.email} app={props.app} {...s} onUnshare={refetchShares} />
            ))}
          </ul>
        )}

        <form onSubmit={handleShareSubmit} className="space-y-4 max-w-[40rem]">
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
      </div>

      <Alert className="max-w-[40rem]">
        <IconHelp className="h-4 w-4" />
        <AlertTitle>How does sharing work?</AlertTitle>
        <AlertDescription className="text-muted-foreground">
          <ol className="list-decimal mx-4 my-2 space-y-1">
            <li>Sharing an app allows other users to have read-only access to your app's dashboards.</li>
            <li>You remain the owner of the app and its billing.</li>
            <li>You can revoke access at any time.</li>
            <li>Shared users cannot modify app settings or transfer ownership.</li>
          </ol>

          <p className="mt-3">
            <span className="font-bold">Note:</span> Aptabase won't send an email as part of the sharing process. We
            recommend contacting users directly and asking them to sign up.
          </p>
        </AlertDescription>
      </Alert>
    </div>
  );
}
