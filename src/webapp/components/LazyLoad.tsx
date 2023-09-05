import { useRef, MutableRefObject, useState, useEffect } from "react";
import { twMerge } from "tailwind-merge";

function useLazyLoad<T extends Element>(
  ref: MutableRefObject<T>,
  rootMargin: string = "0px"
): boolean {
  const [isIntersecting, setIntersecting] = useState<boolean>(false);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !isIntersecting) {
          setIntersecting(entry.isIntersecting);
          observer.unobserve(ref.current);
        }
      },
      {
        rootMargin,
      }
    );

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => {
      if (ref.current instanceof Element) {
        observer.unobserve(ref.current);
      }
    };
  }, []);

  return isIntersecting;
}

type Props = {
  className?: string;
  children: React.ReactNode;
};

export function LazyLoad(props: Props) {
  const ref: any = useRef<HTMLDivElement>();
  const show: boolean = useLazyLoad<HTMLDivElement>(ref);

  return (
    <div ref={ref} className={twMerge("flex flex-col", props.className)}>
      {show && props.children}
    </div>
  );
}
