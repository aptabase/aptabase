import { useState } from "react";
import { AppIconUpload, useApps } from "@app/apps";
import { useCurrentApp, useNavigationContext } from "@app/navigation";
import { Button, TextInput } from "@app/primitives";

export function GeneralSettings() {
  const app = useCurrentApp();
  const { updateApp } = useApps();
  const { switchApp } = useNavigationContext();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const updatedApp = await updateApp(app.id, name, icon);
    switchApp(updatedApp);
  };
  const [name, setName] = useState(app.name);
  const [icon, setIcon] = useState("");

  return (
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
  );
}
