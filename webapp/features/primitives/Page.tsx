import { motion } from "framer-motion";
import { useEffect } from "react";
import { useLocation } from "react-router-dom";

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
    <motion.div
      key={location.pathname}
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.7 }}
    >
      {props.children}
    </motion.div>
  );
}
