import { AppIconUpload, useApps } from "@app/apps";
import { useCurrentApp, useNavigationContext } from "@app/navigation";
import { Button, Head, PageHeading, TextInput } from "@app/primitives";
import { useState } from "react";
import { DangerZone } from "./components/DangerZone";

Component.displayName = "Settings";
export function Component() {
  const app = useCurrentApp();
  const { updateApp } = useApps();
  const { switchApp } = useNavigationContext();
  const [name, setName] = useState(app.name);
  const [icon, setIcon] = useState("");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const updatedApp = await updateApp(app.id, name, icon);
    switchApp(updatedApp);
  };

  return (
    <>
      <Head title={`${app.name} - Settings`} />
      <PageHeading title="Settings" subtitle="Manage your app settings" />

      <form onSubmit={handleSubmit} className="mt-8 space-y-2 w-80">
        <AppIconUpload iconPath={app.iconPath} onIconChanged={setIcon} />

        <TextInput
          label="Name"
          name="name"
          required={true}
          value={name}
          onChange={setName}
        />
        <div className="w-20">
          <Button
            variant="primary"
            disabled={name.length < 2 || name.length > 40}
          >
            Save
          </Button>
        </div>
      </form>

      <DangerZone appId={app.id} appName={app.name} />
    </>
  );
}
