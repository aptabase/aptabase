import { NavCategory, NavItem } from "../navigation";
import { useSupport } from "./useSupport";
import { IconGift, IconHelpCircle, IconMessageCircle } from "@tabler/icons-react";

export function SupportNavCategory() {
  const { toggleChat } = useSupport();

  return (
    <NavCategory title="Product">
      <NavItem label="Help" onClick={toggleChat} icon={IconHelpCircle} />
      <NavItem label="Feedback" onClick={toggleChat} icon={IconMessageCircle} />
      <NavItem label="Affiliates" href="https://aptabase.lemonsqueezy.com/affiliates" icon={IconGift} />
    </NavCategory>
  );
}
