import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { api } from "@fns/api";
import { formatDate } from "@fns/format-date";
import { Dialog, DialogTitle, Transition } from "@headlessui/react";
import { IconCheck, IconCrown, IconX } from "@tabler/icons-react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Fragment } from "react";
import { toast } from "sonner";

type IncomingTransferRequest = {
  appId: string;
  appName: string;
  currentOwnerEmail: string;
  requestedAt: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
};

export function OwnershipTransferRequestsModal(props: Props) {
  const {
    isLoading,
    isError,
    data: requests,
    refetch,
  } = useQuery({
    queryKey: ["ownershipTransferRequests"],
    queryFn: () => api.get<IncomingTransferRequest[]>("/_ownership-transfer-requests"),
    enabled: props.open, // only fetch when modal is open
  });

  const acceptMutation = useMutation({
    mutationFn: (appId: string) => api.post(`/_apps/${appId}/ownership-transfer/accept`),
    onSuccess: () => {
      toast.success("Ownership transfer accepted");
      refetch();
    },
    onError: () => {
      toast.error("Failed to accept ownership transfer");
    },
  });

  const rejectMutation = useMutation({
    mutationFn: (appId: string) => api.post(`/_apps/${appId}/ownership-transfer/reject`),
    onSuccess: () => {
      toast.success("Ownership transfer rejected");
      refetch();
    },
    onError: () => {
      toast.error("Failed to reject ownership transfer");
    },
  });

  // close modal when no requests remain
  const hasRequests = requests && requests.length > 0;
  if (props.open && !isLoading && !hasRequests) {
    props.onClose();
  }

  return (
    <Transition.Root show={props.open} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={props.onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-10 backdrop-blur-sm transition-opacity" />
        </Transition.Child>

        <div className="fixed inset-0 z-10 overflow-y-auto">
          <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
            <Transition.Child
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
              enterTo="opacity-100 translate-y-0 sm:scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 translate-y-0 sm:scale-100"
              leaveTo="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
            >
              <Dialog.Panel className="relative transform overflow-hidden rounded-lg bg-background border px-4 pt-5 pb-4 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-2xl sm:p-6">
                <div className="absolute top-0 right-0 hidden pt-4 pr-4 sm:block">
                  <button
                    type="button"
                    className="rounded-md text-muted-foreground hover:text-foreground"
                    onClick={props.onClose}
                  >
                    <IconX className="h-6 w-6" />
                  </button>
                </div>

                <div className="flex items-center space-x-2 mb-6">
                  <IconCrown className="h-5 w-5 text-warning" />
                  <DialogTitle as="h3" className="text-lg font-medium">
                    Pending Ownership Transfers
                  </DialogTitle>
                </div>

                {isLoading && <LoadingState />}
                {isError && <ErrorState />}

                {requests && requests.length > 0 && (
                  <div className="space-y-4">
                    {requests.map((request) => (
                      <div key={request.appId} className="border border-warning/20 bg-warning/10 rounded-md p-4">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <h4 className="font-semibold">{request.appName}</h4>
                            <p className="text-sm mt-1">
                              <span className="font-medium">{request.currentOwnerEmail}</span> wants to transfer
                              ownership to you
                            </p>
                            <p className="text-xs text-foreground/80 mt-1">
                              Requested {formatDate(request.requestedAt)}
                            </p>
                          </div>

                          <div className="flex space-x-2 ml-6">
                            <Button
                              size="sm"
                              variant="default"
                              onClick={() => acceptMutation.mutate(request.appId)}
                              disabled={acceptMutation.isPending || rejectMutation.isPending}
                              loading={acceptMutation.isPending}
                            >
                              <IconCheck className="h-4 w-4 mr-1" />
                              Accept
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => rejectMutation.mutate(request.appId)}
                              disabled={acceptMutation.isPending || rejectMutation.isPending}
                              loading={rejectMutation.isPending}
                            >
                              <IconX className="h-4 w-4 mr-1" />
                              Reject
                            </Button>
                          </div>
                        </div>

                        <div className="mt-3 p-3 bg-background/50 rounded border">
                          <p className="text-xs text-muted-foreground">
                            <span className="font-semibold">By accepting:</span> You will become the owner and be
                            responsible for billing. The current owner will be added to the sharing list with read-only
                            access.
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition.Root>
  );
}
