import { useEffect, useState } from "react";

export function useDelay(delayMs: number) {
  let [show, setShow] = useState(delayMs === 0);

  useEffect(() => {
    const timer = setTimeout(() => {
      setShow(true);
    }, delayMs);
    return () => clearTimeout(timer);
  });

  return show;
}
