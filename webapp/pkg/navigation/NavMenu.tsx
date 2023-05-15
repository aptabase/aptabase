import {
  IconCode,
  IconHelpCircle,
  IconLayoutGrid,
  IconMessageCircle,
  IconSettings,
  TablerIconsProps,
} from "@tabler/icons-react";
import clsx from "clsx";
import { Link, useLocation } from "react-router-dom";

type NavItem = {
  name: string;
  href?: string;
  onClick?: () => void;
  icon: (props: TablerIconsProps) => JSX.Element;
  disabled?: boolean;
};

type NavCategory = {
  title: string;
  items: NavItem[];
};

const toggleChat = () => {
  window.$crisp?.push(["do", "chat:show"]);
  window.$crisp?.push(["do", "chat:open"]);
};

const navigation: NavCategory[] = [
  {
    title: "",
    items: [
      { name: "Dashboard", href: "/", icon: IconLayoutGrid },
      // {
      //   name: "Insights",
      //   href: "#",
      //   icon: ArrowTrendingUpIcon,
      //   disabled: true,
      // },
      // { name: "Share", href: "#", icon: ShareIcon, disabled: true },
      { name: "Settings", href: "/settings", icon: IconSettings },
      {
        name: "Instructions",
        href: "/instructions",
        icon: IconCode,
      },
    ],
  },
  {
    title: "Resources",
    items: [
      { name: "Support", onClick: toggleChat, icon: IconHelpCircle },
      // {
      //   name: "Documentation",
      //   href: "#",
      //   icon: DocumentTextIcon,
      //   disabled: true,
      // },
      {
        name: "Feedback",
        onClick: toggleChat,
        icon: IconMessageCircle,
      },
    ],
  },
];

const NavLink = (props: {
  current: boolean;
  item: NavItem;
  onNavigation?: VoidFunction;
}) => {
  const baseClassName =
    "group flex items-center rounded-md py-2 lg:py-1 px-2 text-sm";

  const content = (
    <>
      <props.item.icon
        strokeWidth={1.75}
        className="text-secondary mr-2 h-5 w-5 flex-shrink-0"
      />
      {props.item.name}
    </>
  );

  if (props.item.disabled) {
    return (
      <div className={clsx("opacity-40 cursor-not-allowed", baseClassName)}>
        {content}
      </div>
    );
  }

  const className = clsx(
    props.current
      ? "bg-gray-200/70"
      : "w-full text-default hover:bg-gray-200/70",
    baseClassName
  );

  const onButtonClick = () => {
    props.onNavigation?.();
    props.item.onClick?.();
  };

  return props.item.href ? (
    <Link
      to={props.item.href}
      className={className}
      onClick={props.onNavigation}
    >
      {content}
    </Link>
  ) : (
    <button onClick={onButtonClick} className={className}>
      {content}
    </button>
  );
};

export function NavMenu(props: { onNavigation?: VoidFunction }) {
  const location = useLocation();

  return (
    <div className="flex flex-col h-full justify-between">
      <div className="space-y-6">
        {navigation.map((category) => (
          <div key={category.title}>
            {category.title && (
              <span className="text-xs tracking-tight font-semibold ml-2 leading-6 text-secondary">
                {category.title}
              </span>
            )}
            <nav className="space-y-0.5">
              {category.items.map((item) => (
                <NavLink
                  key={item.name}
                  current={item.href === location.pathname}
                  item={item}
                  onNavigation={props.onNavigation}
                />
              ))}
            </nav>
          </div>
        ))}
      </div>
    </div>
  );
}
