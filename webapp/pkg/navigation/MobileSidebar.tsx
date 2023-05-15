import { Dialog, Transition } from "@headlessui/react";
import { Fragment } from "react";
import { NavMenu } from "./NavMenu";
import { IconMenu2, IconX } from "@tabler/icons-react";
import { isBillingEnabled } from "@app/env";
import { CurrentUsage } from "@app/billing";

type ButtonProps = {
  onClick: () => void;
};

MobileSidebar.Button = (props: ButtonProps) => {
  return (
    <button
      type="button"
      className="border-r border-default px-4 text-default lg:hidden"
      onClick={props.onClick}
    >
      <IconMenu2 strokeWidth={2} className="h-6 w-6" />
    </button>
  );
};

type Props = {
  open: boolean;
  onClose: (open: boolean) => void;
};

export function MobileSidebar(props: Props) {
  const close = () => props.onClose(false);

  return (
    <Transition.Root show={props.open} as={Fragment}>
      <Dialog
        as="div"
        className="relative z-40 lg:hidden"
        onClose={props.onClose}
      >
        <Transition.Child
          as={Fragment}
          enter="transition-opacity ease-linear duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="transition-opacity ease-linear duration-300"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black opacity-75" />
        </Transition.Child>

        <div className="fixed inset-0 z-40 flex">
          <Transition.Child
            as={Fragment}
            enter="transition ease-in-out duration-300 transform"
            enterFrom="-translate-x-full"
            enterTo="translate-x-0"
            leave="transition ease-in-out duration-300 transform"
            leaveFrom="translate-x-0"
            leaveTo="-translate-x-full"
          >
            <Dialog.Panel className="relative flex w-full max-w-xs flex-1 flex-col bg-white pt-2 pb-4">
              <Transition.Child
                as={Fragment}
                enter="ease-in-out duration-300"
                enterFrom="opacity-0"
                enterTo="opacity-100"
                leave="ease-in-out duration-300"
                leaveFrom="opacity-100"
                leaveTo="opacity-0"
              >
                <div className="absolute top-0 right-0 -mr-12 pt-2">
                  <button
                    type="button"
                    className="ml-1 flex h-10 w-10 items-center justify-center rounded-full"
                    onClick={close}
                  >
                    <IconX className="h-6 w-6 text-white" />
                  </button>
                </div>
              </Transition.Child>
              <div className="p-2">
                <NavMenu onNavigation={close} />
              </div>
              {isBillingEnabled && (
                <div className="px-2 py-4 border-t border-default">
                  <CurrentUsage />
                </div>
              )}
            </Dialog.Panel>
          </Transition.Child>
          <div className="w-14 flex-shrink-0">
            {/* Dummy element to force sidebar to shrink to fit close icon */}
          </div>
        </div>
      </Dialog>
    </Transition.Root>
  );
}
