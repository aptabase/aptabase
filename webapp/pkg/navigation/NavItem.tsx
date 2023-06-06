import { TablerIconsProps } from "@tabler/icons-react";
import clsx from "clsx";
import { Link, useLocation } from "react-router-dom";

type Props = {
  label: string;
  href?: string;
  onClick?: () => void;
  icon: (props: TablerIconsProps) => JSX.Element;
  onNavigation?: VoidFunction;
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
    props.href === location.pathname
      ? "bg-emphasis"
      : "w-full hover:text-inverted hover:bg-emphasis",
    baseClassName
  );

  const onButtonClick = () => {
    props.onNavigation?.();
    props.onClick?.();
  };

  return props.href ? (
    <Link to={props.href} className={className} onClick={props.onNavigation}>
      {content}
    </Link>
  ) : (
    <button onClick={onButtonClick} className={className}>
      {content}
    </button>
  );
}
