import { Alert, AlertDescription, AlertTitle } from "@components/Alert";
import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { TextInput } from "@components/TextInput";
import { Application } from "@features/apps";
import { api } from "@fns/api";
import { IconClock, IconCrown, IconHelp } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { OwnershipTransferModal } from "./OwnershipTransferModal";

type OwnershipTransferRequest = {
  newOwnerEmail: string;
  requestedAt: string;
  status: "pending" | "accepted" | "rejected";
};

type Props = {
  app: Application;
};

export function OwnershipTransfer(props: Props) {
  const [newOwnerEmail, setNewOwnerEmail] = useState("");
  const [showTransferModal, setShowTransferModal] = useState(false);

  const {
    isLoading: transferLoading,
    isError: transferError,
    data: ownershipTransfer,
    refetch: refetchTransfer,
  } = useQuery({
    queryKey: ["ownership-transfer", props.app.id],
    queryFn: async () => {
      try {
        return await api.get<OwnershipTransferRequest | null>(`/_apps/${props.app.id}/ownership-transfer`);
      } catch (error: any) {
        // treat 204 No Content as successful null response
        if (error.response?.status === 204) {
          return null;
        }
        throw error;
      }
    },
  });

  const handleTransferSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setShowTransferModal(true);
  };

  const handleCancelTransfer = async () => {
    await api.delete(`/_apps/${props.app.id}/ownership-transfer`);
    refetchTransfer();
  };

  const handleTransferInitiated = () => {
    setNewOwnerEmail("");
    refetchTransfer();
  };

  if (transferLoading) return <LoadingState />;
  if (transferError) return <ErrorState />;

  const hasPendingTransfer = ownershipTransfer?.status === "pending";

  return (
    <div className="space-y-8">
      {/* Ownership Transfer Section */}
      <div className="max-w-[40rem]">
        <div className="flex items-center space-x-2 mb-4">
          <IconCrown className="h-5 w-5 text-foreground" />
          <h3 className="text-lg font-medium">Transfer Ownership</h3>
        </div>

        {hasPendingTransfer ? (
          <div className="border border-warning/20 bg-warning/10 rounded-md p-4">
            <div className="flex items-center space-x-2 mb-2">
              <IconClock className="h-4 w-4 text-warning" />
              <p className="font-medium text-warning-foreground">Transfer Pending</p>
            </div>
            <p className="text-sm text-warning-foreground mb-3">
              Ownership transfer to <span className="font-semibold">{ownershipTransfer.newOwnerEmail}</span> is pending
              approval.
            </p>
            <Button variant="outline" size="sm" onClick={handleCancelTransfer}>
              Cancel Transfer
            </Button>
          </div>
        ) : (
          <form onSubmit={handleTransferSubmit} className="space-y-4">
            <div className="flex items-center space-x-2">
              <TextInput
                label="Transfer to:"
                name="newOwnerEmail"
                type="email"
                required={true}
                value={newOwnerEmail}
                placeholder="new.owner@corp.com"
                maxLength={300}
                onChange={(e) => setNewOwnerEmail(e.target.value)}
                description="Enter the email of the user who will become the new owner."
              />
              <Button variant="destructive" disabled={newOwnerEmail.length === 0} type="submit">
                Transfer Ownership
              </Button>
            </div>
          </form>
        )}
      </div>

      <Alert className="max-w-[40rem]">
        <IconHelp className="h-4 w-4" />
        <AlertTitle>How does ownership transfer work?</AlertTitle>
        <AlertDescription className="text-muted-foreground">
          <ol className="list-decimal mx-4 my-2 space-y-1">
            <li>Transferring ownership gives another user full control of the app.</li>
            <li>The new owner becomes responsible for billing and management.</li>
            <li>You will be added to the sharing list with read-only access.</li>
            <li>This action requires approval from the new owner.</li>
            <li>
              <span className="font-bold">This action cannot be undone</span> once accepted.
            </li>
          </ol>

          <p className="mt-3">
            <span className="font-bold">Note:</span> Aptabase won't send an email as part of the transfer process. We
            recommend contacting the new owner directly to let them know about the pending request.
          </p>
        </AlertDescription>
      </Alert>

      <OwnershipTransferModal
        open={showTransferModal}
        onClose={() => setShowTransferModal(false)}
        app={props.app}
        newOwnerEmail={newOwnerEmail}
        onTransferInitiated={handleTransferInitiated}
      />
    </div>
  );
}
