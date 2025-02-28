import { Button } from "@components/Button";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { IconGripVertical, IconMinus, IconPlus } from "@tabler/icons-react";
import { ReactNode } from "react";

type Props = {
  children: ReactNode;
  className?: string;
  isMinimized?: boolean;
  onToggleMinimize: () => void;
  id: string;
  widgetName: string;
};

export const WidgetContainer = ({ children, className = "", isMinimized, onToggleMinimize, id, widgetName }: Props) => {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
    id,
    animateLayoutChanges: () => false,
  });

  const style = {
    transform: transform ? CSS.Translate.toString(transform) : undefined,
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`relative rounded-lg border border-border ${className} ${isMinimized ? "py-2" : ""}`}
    >
      {isMinimized && <span className="absolute -top-3 left-3 bg-background font-medium px-1">{widgetName}</span>}

      <div className="absolute -top-3 right-3 bg-background px-1 flex gap-1">
        <Button
          variant="ghost"
          size="xs"
          className="h-6 w-6 rounded-full hover:bg-accent cursor-grab"
          {...attributes}
          {...listeners}
        >
          <IconGripVertical className="w-4 h-4" />
        </Button>
        <Button variant="ghost" size="xs" className="h-6 w-6 rounded-full hover:bg-accent" onClick={onToggleMinimize}>
          {isMinimized ? <IconPlus className="w-4 h-4" /> : <IconMinus className="w-4 h-4" />}
        </Button>
      </div>
      <div className={`p-4 ${isMinimized ? "hidden" : ""}`}>{children}</div>
    </div>
  );
};
