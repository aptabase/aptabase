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
    if (processing) return;
    setProcessing(true);
    setError("");
    try {
      const updatedApp = await updateApp(props.app.id, name, icon, retentionChanged ? retentionMonths : undefined);
      navigate(`/${updatedApp.id}/`);
    } catch {
      setError("Could not save your changes. Please try again.");
    } finally {
      setProcessing(false);
    }
  };

  const [name, setName] = useState(props.app.name);
  const [icon, setIcon] = useState("");
  const [retentionMonths, setRetentionMonths] = useState(props.app.retentionMonths);
  const [retentionChanged, setRetentionChanged] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState("");

  return (
    <form onSubmit={handleSubmit} className="max-w-[30rem]">
      <fieldset disabled={processing} className="space-y-4">
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

        <RetentionSelect value={retentionMonths} disabled={processing} onChange={(value) => {
          setRetentionMonths(value);
          setRetentionChanged(true);
        }} />

        {error && <p role="alert" className="text-sm text-destructive">{error}</p>}

        <div className="w-20">
          <Button disabled={name.length < 2 || processing} loading={processing}>Save</Button>
        </div>
      </fieldset>
    </form>
  );
}
