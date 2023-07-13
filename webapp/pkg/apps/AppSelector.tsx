import clsx from "clsx";
import { Application, useApps } from "@app/apps";
import { Listbox, Transition } from "@headlessui/react";
import { Fragment, useState } from "react";
import { CreateAppModal } from "./CreateAppModal";
import { useNavigationContext } from "@app/navigation";
import { IconBox, IconPlus, IconSelector } from "@tabler/icons-react";
import { useNavigate } from "react-router-dom";
import { AppIcon } from "./AppIcon";

type OptionProps = {
  value?: Application;
  children: React.ReactNode;
};

function StyledOption(props: OptionProps) {
  return (
    <Listbox.Option
      className={({ active }) =>
        clsx(
          active ? "text-foreground bg-accent" : "",
          "relative rounded cursor-pointer select-none p-2 mx-1 text-sm flex items-center space-x-1"
        )
      }
      value={props.value}
    >
      {props.children}
    </Listbox.Option>
  );
}

const Divider = () => <div className="border-t my-1" />;

export function AppSelector() {
  const { apps } = useApps();
  const navigate = useNavigate();
  const { currentApp, switchApp } = useNavigationContext();

  const [showCreateAppModal, setShowCreateAppModal] = useState(false);

  const onChange = async (app: Application | undefined) => {
    if (!app) {
      return setShowCreateAppModal(true);
    }

    switchApp(app);
    navigate("/");
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
              <Listbox.Button className="relative flex items-center space-x-1 rounded-md py-1.5 text-left hover:bg-accent px-2">
                <AppIcon className="w-5 h-5" iconPath={currentApp.iconPath} />
                <div className="flex items-center space-x-2">
                  <span className="block truncate w-44">{currentApp.name}</span>
                  <IconSelector strokeWidth={2} className="h-4 w-4" />
                </div>
              </Listbox.Button>

              <Transition
                show={open}
                as={Fragment}
                leave="transition ease-in duration-100"
                leaveFrom="opacity-100"
                leaveTo="opacity-0"
              >
                <Listbox.Options className="absolute z-10 mt-1 max-h-80 w-60 overflow-auto border rounded-md bg-background py-1 shadow-lg text-sm focus-ring">
                  {apps.map((app) => (
                    <StyledOption key={app.id} value={app}>
                      <AppIcon
                        className="w-5 h-5 mr-1"
                        iconPath={app.iconPath}
                      />
                      <span className="block truncate">{app.name}</span>
                    </StyledOption>
                  ))}
                  <Divider />
                  <StyledOption>
                    <IconPlus strokeWidth={1.75} className="h-4 w-4" />
                    <span className="tracking-tight">Create new App</span>
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
