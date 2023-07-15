import { Application, useApps } from "@app/apps";
import { Button } from "@app/primitives";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

type Props = {
  app: Application;
};

export function DangerZone(props: Props) {
  const { deleteApp } = useApps();
  const navigate = useNavigate();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (
      window.confirm(
        `Are you really sure you want to delete application '${props.app.name}'?`
      )
    ) {
      await deleteApp(props.app.id);
      toast(`${props.app.name} app was deleted.`);
      navigate("/");
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="border border-destructive rounded p-4 max-w-md"
    >
      <h2 className="text-lg">Delete {props.app.name}?</h2>
      <div className="text-sm text-muted-foreground">
        This will permanently delete the {props.app.name} app and its events.
      </div>
      <div className="mt-4">
        <div className="w-20">
          <Button variant="destructive">Delete</Button>
        </div>
      </div>
    </form>
  );
}
