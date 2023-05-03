import {
  ArrowTrendingUpIcon,
  ChatBubbleBottomCenterTextIcon,
  CodeBracketSquareIcon,
  Cog8ToothIcon,
  DocumentTextIcon,
  QuestionMarkCircleIcon,
  ShareIcon,
  Squares2X2Icon,
} from "@heroicons/react/24/outline";
import clsx from "clsx";
import { Link, useLocation } from "react-router-dom";

type NavItem = {
  name: string;
  href?: string;
  onClick?: () => void;
  icon: React.ForwardRefExoticComponent<
    Omit<React.SVGProps<SVGSVGElement>, "ref">
  >;
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
      { name: "Dashboard", href: "/", icon: Squares2X2Icon },
      // {
      //   name: "Insights",
      //   href: "#",
      //   icon: ArrowTrendingUpIcon,
      //   disabled: true,
      // },
      // { name: "Share", href: "#", icon: ShareIcon, disabled: true },
      { name: "Settings", href: "/settings", icon: Cog8ToothIcon },
      {
        name: "Instructions",
        href: "/instructions",
        icon: CodeBracketSquareIcon,
      },
    ],
  },
  {
    title: "Resources",
    items: [
      { name: "Support", onClick: toggleChat, icon: QuestionMarkCircleIcon },
      // {
      //   name: "Documentation",
      //   href: "#",
      //   icon: DocumentTextIcon,
      //   disabled: true,
      // },
      {
        name: "Feedback",
        onClick: toggleChat,
        icon: ChatBubbleBottomCenterTextIcon,
      },
    ],
  },
];

const NavLink = (props: { current: boolean; item: NavItem }) => {
  const baseClassName =
    "group flex items-center rounded-md py-2 lg:py-1 px-2 text-sm";

  const content = (
    <>
      <props.item.icon className="text-secondary mr-2 h-5 w-5 flex-shrink-0" />
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

  return props.item.href ? (
    <Link to={props.item.href} className={className}>
      {content}
    </Link>
  ) : (
    <button onClick={props.item.onClick} className={className}>
      {content}
    </button>
  );
};

export function NavMenu() {
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
                />
              ))}
            </nav>
          </div>
        ))}
      </div>
    </div>
  );
}
