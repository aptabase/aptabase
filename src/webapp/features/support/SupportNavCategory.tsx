import { IconGift, IconMessageCircle } from "@tabler/icons-react";
import { NavCategory, NavItem } from "../navigation";
import { useSupport } from "./useSupport";

export function SupportNavCategory() {
  const { toggleChat } = useSupport();

  return (
    <NavCategory title="Product">
      <NavItem label="Help & Feedback" onClick={toggleChat} icon={IconMessageCircle} />
      <NavItem label="Affiliates" href="https://aptabase.lemonsqueezy.com/affiliates" icon={IconGift} />
    </NavCategory>
  );
}
