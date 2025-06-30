import { Button } from "@components/Button";
import { Application } from "@features/apps";
import { api } from "@fns/api";
import { Dialog, DialogPanel, DialogTitle, Transition, TransitionChild } from "@headlessui/react";
import { IconAlertTriangle, IconX } from "@tabler/icons-react";
import { Fragment, useState } from "react";
import { toast } from "sonner";
import { AppRequestPurpose } from "./app-requests";

type Props = {
  open: boolean;
  onClose: () => void;
  app: Application;
  newOwnerEmail: string;
  onTransferInitiated: () => void;
};

export function OwnershipTransferModal(props: Props) {
  const [processing, setIsProcessing] = useState(false);

  const close = () => {
    setIsProcessing(false);
    props.onClose();
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsProcessing(true);

    try {
      await api.post(`/_apps/${props.app.id}/requests`, {
        targetUserEmail: props.newOwnerEmail.trim(),
        purpose: AppRequestPurpose.AppOwnership,
      });
      props.onTransferInitiated();
      close();
    } catch (error) {
      setIsProcessing(false);
      toast.error("Failed: " + error);
    }
  };

  return (
    <Transition show={props.open} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={close}>
        <TransitionChild
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-10 backdrop-blur-sm transition-opacity" />
        </TransitionChild>

        <div className="fixed inset-0 z-10 overflow-y-auto">
          <div className="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
            <TransitionChild
              as={Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
              enterTo="opacity-100 translate-y-0 sm:scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 translate-y-0 sm:scale-100"
              leaveTo="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
            >
              <DialogPanel className="relative transform overflow-hidden rounded-lg bg-background border px-4 pt-5 pb-4 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-md sm:p-6">
                <div className="absolute top-0 right-0 hidden pt-4 pr-4 sm:block">
                  <button
                    type="button"
                    className="rounded-md text-muted-foreground hover:text-foreground"
                    onClick={close}
                  >
                    <IconX className="h-6 w-6" />
                  </button>
                </div>

                <div className="flex items-center space-x-3">
                  <IconAlertTriangle className="h-6 w-6 text-warning" />
                  <DialogTitle as="h3" className="text-lg font-medium text-warning">
                    Transfer Ownership
                  </DialogTitle>
                </div>

                <form onSubmit={handleSubmit} className="mt-4">
                  <div className="space-y-4">
                    <p className="text-sm text-muted-foreground">
                      You are about to transfer ownership of <span className="font-semibold">{props.app.name}</span> to{" "}
                      <span className="font-semibold">{props.newOwnerEmail}</span>.
                    </p>

                    <div className="bg-warning/10 border border-warning/20 rounded-md p-3">
                      <p className="text-sm text-warning-foreground">
                        <span className="font-semibold">This action cannot be undone.</span> Once the new owner accepts,
                        you will:
                      </p>
                      <ul className="list-disc ml-4 mt-2 text-sm text-warning-foreground space-y-1">
                        <li>Lose ownership and billing responsibility</li>
                        <li>Be added to the sharing list with read-only access</li>
                        <li>No longer be able to manage app settings</li>
                      </ul>
                    </div>

                    <p className="text-sm text-muted-foreground">
                      The new owner will need to accept this transfer request before ownership changes.
                    </p>
                  </div>

                  <div className="mt-6 flex space-x-3">
                    <Button variant="destructive" disabled={processing} loading={processing} type="submit">
                      Transfer Ownership
                    </Button>
                    <Button variant="ghost" onClick={close} disabled={processing}>
                      Cancel
                    </Button>
                  </div>
                </form>
              </DialogPanel>
            </TransitionChild>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
}
