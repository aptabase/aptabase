import { NavCategory, NavItem } from "@app/navigation";
import { useSupport } from "./useSupport";
import { IconHelpCircle, IconMessageCircle } from "@tabler/icons-react";

export function SupportNavCategory() {
  const { toggleChat } = useSupport();

  return (
    <NavCategory title="Support">
      <NavItem label="Help" onClick={toggleChat} icon={IconHelpCircle} />
      <NavItem label="Feedback" onClick={toggleChat} icon={IconMessageCircle} />
    </NavCategory>
  );
}
