import { TablerIconsProps } from "@tabler/icons-react";
import clsx from "clsx";
import { NavLink, useLocation } from "react-router-dom";

type Props = {
  label: string;
  href?: string;
  onClick?: () => void;
  icon: (props: TablerIconsProps) => JSX.Element;
  onNavigation?: VoidFunction;
  disabled?: boolean;
};

export function NavItem(props: Props) {
  const location = useLocation();
  const baseClassName = "group flex items-center rounded-md py-2 px-2 text-sm";

  const content = (
    <>
      <props.icon strokeWidth={1.75} className="mr-2 h-5 w-5 flex-shrink-0" />
      {props.label}
    </>
  );

  const className = clsx(
    props.href === location.pathname ? "bg-accent" : "w-full hover:bg-accent",
    baseClassName
  );

  const onButtonClick = () => {
    props.onNavigation?.();
    props.onClick?.();
  };

  return props.disabled ? (
    <span className={clsx(baseClassName, "text-muted-foreground")}>
      {content}
    </span>
  ) : props.href ? (
    <NavLink
      to={props.href}
      className={({ isActive }) => clsx(className, isActive && "bg-accent")}
      onClick={props.onNavigation}
    >
      {content}
    </NavLink>
  ) : (
    <button onClick={onButtonClick} className={className}>
      {content}
    </button>
  );
}
