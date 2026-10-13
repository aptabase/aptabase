import { useState } from "react";
import { AppIconUpload, Application, useApps } from "@features/apps";
import { useNavigate } from "react-router-dom";
import { Button } from "@components/Button";
import { TextInput } from "@components/TextInput";
import { RetentionSelect } from "./RetentionSelect";

type Props = {
  app: Application;
};

export function GeneralSettings(props: Props) {
  const { updateApp } = useApps();
  const navigate = useNavigate();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const updatedApp = await updateApp(props.app.id, name, icon, retentionMonths);
    navigate(`/${updatedApp.id}/`);
  };

  const [name, setName] = useState(props.app.name);
  const [icon, setIcon] = useState("");
  const [retentionMonths, setRetentionMonths] = useState(props.app.retentionMonths);

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-[30rem]">
      <AppIconUpload iconPath={props.app.iconPath} onIconChanged={setIcon} />

      <TextInput
        label="Name"
        name="name"
        required={true}
        value={name}
        maxLength={40}
        onChange={(e) => setName(e.target.value)}
        description="A friendly name to identify your app. You can change at any time."
      />

      <RetentionSelect value={retentionMonths} onChange={setRetentionMonths} />

      <div className="w-20">
        <Button disabled={name.length < 2}>Save</Button>
      </div>
    </form>
  );
}
