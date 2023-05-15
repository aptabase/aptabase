import { Application, useApps } from "@app/apps";
import { Listbox, Transition } from "@headlessui/react";
import clsx from "clsx";
import { Fragment, useState } from "react";
import { CreateAppModal } from "./CreateAppModal";
import { useNavigationContext } from "./NavigationProvider";
import { IconBox, IconPlus, IconSelector } from "@tabler/icons-react";

type OptionProps = {
  value?: Application;
  children: React.ReactNode;
};

function StyledOption(props: OptionProps) {
  return (
    <Listbox.Option
      className={({ active }) =>
        clsx(
          active ? "bg-gray-100" : "",
          "relative rounded cursor-pointer select-none p-2 mx-1 text-sm flex items-center space-x-2"
        )
      }
      value={props.value}
    >
      {props.children}
    </Listbox.Option>
  );
}

const Divider = () => <div className="border-t my-1 border-default" />;

export function AppSelector() {
  const { apps } = useApps();
  const { currentApp, switchApp } = useNavigationContext();

  const [showCreateAppModal, setShowCreateAppModal] = useState(false);

  const onChange = async (app: Application | undefined) => {
    if (!app) {
      return setShowCreateAppModal(true);
    }

    switchApp(app);
  };

  if (!currentApp) {
    return <div className="h-8" />;
  }

  return (
    <>
      <CreateAppModal
        open={showCreateAppModal}
        onClose={() => setShowCreateAppModal(false)}
      />
      <Listbox value={currentApp} onChange={onChange}>
        {({ open }) => (
          <>
            <div className="relative">
              <Listbox.Button className="relative flex items-center space-x-1 rounded-md py-1.5 text-left hover:bg-gray-200/70 text-base px-2">
                <IconBox
                  strokeWidth={1.75}
                  className="h-5 w-5 p-0.5 bg-white border rounded border-default text-secondary"
                />
                <div className="flex items-center space-x-2">
                  <span className="block">{currentApp?.name}</span>
                  <IconSelector
                    strokeWidth={2}
                    className="h-4 w-4 text-secondary"
                  />
                </div>
              </Listbox.Button>

              <Transition
                show={open}
                as={Fragment}
                leave="transition ease-in duration-100"
                leaveFrom="opacity-100"
                leaveTo="opacity-0"
              >
                <Listbox.Options className="absolute z-10 mt-1 max-h-80 w-60 overflow-auto border border-default rounded-md bg-white py-1 shadow-lg text-sm focus-ring">
                  {apps.map((app) => (
                    <StyledOption key={app.id} value={app}>
                      <span className="vfont-normal block truncate">
                        {app.name}
                      </span>
                    </StyledOption>
                  ))}
                  <Divider />
                  <StyledOption>
                    <IconPlus strokeWidth={1.75} className="h-4 w-4" />
                    <span className="tracking-tight uppercase font-semibold">
                      Create new App
                    </span>
                  </StyledOption>
                </Listbox.Options>
              </Transition>
            </div>
          </>
        )}
      </Listbox>
    </>
  );
}
