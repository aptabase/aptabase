import { Button } from "@components/Button";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { IconGripVertical, IconMinus, IconPlus, IconX } from "@tabler/icons-react";
import { ReactNode } from "react";
import { SingleWidgetConfig } from "../../../atoms/widgets-atoms";

type Props = {
  children: ReactNode;
  className?: string;
  widgetName: string;
  widgetConfig: SingleWidgetConfig;
  onToggleMinimize: () => void;
  onRemove?: () => void;
};

export const WidgetContainer = ({
  children,
  className = "",
  widgetName,
  widgetConfig,
  onToggleMinimize,
  onRemove,
}: Props) => {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
    id: widgetConfig.id,
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
      className={`relative rounded-lg border border-border ${className} ${widgetConfig.isMinimized ? "py-2" : ""}`}
    >
      {widgetConfig.isMinimized && (
        <span className="absolute -top-3 left-3 bg-background font-medium px-1">{widgetName}</span>
      )}

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
        {widgetConfig.isDefined && (
          <Button variant="ghost" size="xs" className="h-6 w-6 rounded-full hover:bg-accent" onClick={onToggleMinimize}>
            {widgetConfig.isMinimized ? <IconPlus className="w-4 h-4" /> : <IconMinus className="w-4 h-4" />}
          </Button>
        )}
        {widgetConfig.supportsRemove && widgetConfig.isDefined && (
          <Button variant="ghost" size="xs" className="h-6 w-6 rounded-full hover:bg-accent" onClick={onRemove}>
            <IconX className="w-4 h-4" />
          </Button>
        )}
      </div>
      <div className={`p-4 ${widgetConfig.isMinimized && widgetConfig.isDefined ? "hidden" : ""}`}>{children}</div>
    </div>
  );
};
