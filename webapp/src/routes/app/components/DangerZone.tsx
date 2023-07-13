import { useApps } from "@app/apps";
import { useCurrentApp } from "@app/navigation";
import { Button } from "@app/primitives";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

export function DangerZone() {
  const app = useCurrentApp();
  const { deleteApp } = useApps();
  const navigate = useNavigate();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (
      window.confirm(
        `Are you really sure you want to delete application '${app.name}'?`
      )
    ) {
      await deleteApp(app.id);
      toast(`${app.name} app was deleted.`);
      navigate("/");
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="border border-destructive rounded p-4 max-w-sm"
    >
      <h2 className="text-lg">Delete App</h2>
      <div className="text-sm text-muted-foreground">
        This will permanently delete this app and its events.
      </div>
      <div className="mt-4">
        <div className="w-20">
          <Button variant="danger">Delete</Button>
        </div>
      </div>
    </form>
  );
}
