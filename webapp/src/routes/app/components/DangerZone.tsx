import { useApps } from "@app/apps";
import { Button } from "@app/primitives";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

type Props = {
  appId: string;
  appName: string;
};

export function DangerZone(props: Props) {
  const { deleteApp } = useApps();
  const navigate = useNavigate();

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (window.confirm(`Are you really sure you want to delete application '${props.appName}'?`)) {
      await deleteApp(props.appId);
      toast(`${props.appName} app was deleted.`);
      navigate("/");
    }
  };

  return (
    <form onSubmit={handleSubmit} className="mt-40 border border-red-600 rounded bg-white p-4 max-w-sm">
      <h2 className="text-lg">Delete App</h2>
      <div className="text-sm text-secondary">This will permanently delete this app and its events.</div>
      <div className="mt-4">
        <div className="w-20">
          <Button variant="danger">Delete</Button>
        </div>
      </div>
    </form>
  );
}
