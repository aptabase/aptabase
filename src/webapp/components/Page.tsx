import { useEffect } from "react";
import { useLocation } from "react-router-dom";
import { LazyMotion, domAnimation, m } from "framer-motion";
import { twJoin } from "tailwind-merge";

type Props = {
  title: string;
  children: React.ReactNode;
};

export function Page(props: Props) {
  const location = useLocation();

  useEffect(() => {
    document.title = `${props.title} · Aptabase`;
  }, [props.title]);

  return (
    <LazyMotion features={domAnimation}>
      <m.div
        key={location.pathname}
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.7 }}
      >
        {props.children}
      </m.div>
    </LazyMotion>
  );
}

type PageHeadingProps = {
  title: string;
  subtitle?: string;
  onClick?: VoidFunction;
};

export function PageHeading(props: PageHeadingProps) {
  return (
    <div>
      <h1
        className={twJoin("text-2xl font-medium", !!props.onClick && "cursor-pointer")}
        onClick={props.onClick}
      >
        {props.title}
      </h1>
      {props.subtitle && <p className="text-muted-foreground">{props.subtitle}</p>}
    </div>
  );
}
