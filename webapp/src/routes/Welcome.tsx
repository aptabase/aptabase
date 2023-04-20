import { useApps } from "@app/apps";
import { useAuth } from "@app/auth";
import { useNavigationContext } from "@app/navigation";
import { Button, TextInput } from "@app/primitives";
import { useState } from "react";
import { Navigate } from "react-router-dom";

Component.displayName = "Welcome";
export function Component() {
  const user = useAuth();
  const { createApp } = useApps();
  const { currentApp } = useNavigationContext();
  const [name, setName] = useState("");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await createApp(name);
  };

  if (currentApp) {
    return <Navigate to="/instructions" />;
  }

  return (
    <div className="mx-auto pt-8 lg:pt-24 max-w-3xl text-base">
      <h2 className="text-3xl font-bold text-default sm:text-4xl">👋 Hey {user.name}</h2>
      <p className="mt-8">Register your Application and configure the analytics SDK to get started.</p>

      <form onSubmit={handleSubmit} className="mt-8 space-y-2 w-80">
        <TextInput
          label="What's the name of the app?"
          name="name"
          placeholder="My App Name"
          required={true}
          value={name}
          onChange={setName}
        />
        <div className="w-20">
          <Button variant="primary" disabled={name.length < 2 || name.length > 40}>
            Create
          </Button>
        </div>
      </form>
    </div>
  );
}
