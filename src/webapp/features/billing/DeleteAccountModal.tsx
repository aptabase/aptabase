import { Button } from "@components/Button";
import { TextInput } from "@components/TextInput";
import { signOut } from "@features/auth/auth";
import { api } from "@fns/api";
import { Dialog, Transition } from "@headlessui/react";
import { IconX } from "@tabler/icons-react";
import { Fragment, useState } from "react";

async function deleteAccount(): Promise<boolean> {
  const [status] = await api.fetch("POST", `/_auth/account/delete`);

  if (status === 200) {
    return true;
  }

  return false;
}

type Props = {
  open: boolean;
  onClose: () => void;
};

export function DeleteAccountModal(props: Props) {
  const [confirmation, setConfirmation] = useState("");
  const [processing, setIsProcessing] = useState(false);

  const close = () => {
    setConfirmation("");
    setIsProcessing(false);
    props.onClose();
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsProcessing(true);

    const deleteResult = await deleteAccount();
    if (deleteResult) {
      await signOut();
    }

    return close();
  };

  return (
    <Transition.Root show={props.open} as={Fragment}>
      <Dialog as="div" className="relative z-50" onClose={close}>
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
              <Dialog.Panel className="relative transform overflow-hidden rounded-lg bg-background border px-4 pt-5 pb-4 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-md sm:p-6">
                <div className="absolute top-0 right-0 hidden pt-4 pr-4 sm:block">
                  <button
                    type="button"
                    className="rounded-md text-muted-foreground hover:text-foreground"
                    onClick={props.onClose}
                  >
                    <IconX className="h-6 w-6" />
                  </button>
                </div>
                <Dialog.Title as="h3" className="text-lg font-medium text-destructive">
                  Delete Account
                </Dialog.Title>
                <form onSubmit={handleSubmit} className="mt-2">
                  <p className="text-sm text-muted-foreground mb-4">
                    This action cannot be undone. This will permanently delete your account and remove all associated
                    data from our servers.
                  </p>
                  <TextInput
                    name="confirmation"
                    placeholder="Type 'DELETE' to confirm"
                    autoComplete="off"
                    required={true}
                    value={confirmation}
                    onChange={(e) => setConfirmation(e.target.value)}
                  />
                  <Button
                    className="mt-4"
                    variant="destructive"
                    disabled={confirmation !== "DELETE" || processing}
                    loading={processing}
                  >
                    Delete Account
                  </Button>
                </form>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition.Root>
  );
}
