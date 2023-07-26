import { useApps } from "@features/apps";
import { useAuth } from "@features/auth";
import { Button, TextInput } from "@features/primitives";
import { useState } from "react";
import { useNavigate } from "react-router-dom";

export function LonelyState() {
  const user = useAuth();
  const { createApp } = useApps();
  const navigate = useNavigate();
  const [name, setName] = useState("");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const app = await createApp(name);
    navigate(`${app.id}/instructions`);
  };

  return (
    <div className="mx-auto pt-8 lg:pt-24 max-w-3xl text-base">
      <h2 className="text-3xl font-bold sm:text-4xl">👋 Hey {user.name}</h2>
      <p className="mt-8 text-muted-foreground">
        Register your application and configure the analytics SDK to get
        started. <br /> It doesn't take long!
      </p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-2 w-80">
        <TextInput
          label="What's your app name?"
          name="name"
          placeholder="Awesome App"
          required={true}
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <div className="w-20">
          <Button disabled={name.length < 2 || name.length > 40}>Create</Button>
        </div>
      </form>
    </div>
  );
}
