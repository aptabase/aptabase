import { IconCubePlus } from "@tabler/icons-react";
import { useState } from "react";
import { CreateAppModal } from "./CreateAppModal";

export function NewAppWidget() {
  const [showModal, setShowModal] = useState(false);
  const openModal = () => setShowModal(true);
  const closeModal = () => setShowModal(false);

  return (
    <>
      <CreateAppModal open={showModal} onClose={closeModal} />
      <button
        onClick={openModal}
        className="border cursor-pointer border-dashed rounded shadow-md h-full group hover:bg-muted"
      >
        <div className="flex flex-col h-full justify-center items-center space-y-1">
          <IconCubePlus stroke={1.5} className="w-6 h-6 text-muted-foreground group-hover:text-foreground" />
          <p className="text-muted-foreground text-sm group-hover:text-foreground">Register a new app</p>
        </div>
      </button>
    </>
  );
}
