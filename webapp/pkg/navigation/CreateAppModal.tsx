import { useApps } from "@app/apps";
import { Button, TextInput } from "@app/primitives";
import { Dialog, Transition } from "@headlessui/react";
import { XMarkIcon } from "@heroicons/react/24/outline";
import { Fragment, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useNavigationContext } from ".";

type Props = {
  open: boolean;
  onClose: () => void;
};

export function CreateAppModal(props: Props) {
  const { createApp } = useApps();
  const { switchApp } = useNavigationContext();
  const navigate = useNavigate();
  const [name, setName] = useState("");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const app = await createApp(name);
    switchApp(app);
    navigate("/instructions");
    props.onClose();
    return;
  };

  return (
    <Transition.Root show={props.open} as={Fragment}>
      <Dialog as="div" className="relative z-10" onClose={props.onClose}>
        <Transition.Child
          as={Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-gray-500 bg-opacity-10 backdrop-blur-sm transition-opacity" />
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
              <Dialog.Panel className="relative transform overflow-hidden rounded-lg bg-white px-4 pt-5 pb-4 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-sm sm:p-6">
                <div className="absolute top-0 right-0 hidden pt-4 pr-4 sm:block">
                  <button type="button" className="rounded-md bg-white text-secondary hover:text-gray-800" onClick={props.onClose}>
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>
                <Dialog.Title as="h3" className="text-lg font-medium">
                  Create an app
                </Dialog.Title>
                <div className="text-sm text-secondary">Each app has its own dashboard and metrics</div>
                <form onSubmit={handleSubmit} className="mt-8 space-y-2">
                  <TextInput label="App Name" name="name" placeholder="My App Name" required={true} value={name} onChange={setName} />
                  <Button variant="primary" disabled={name.length < 2 || name.length > 40}>
                    Create
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
