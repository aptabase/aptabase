type Props = {
  children: React.ReactNode;
};

export function KeyMetricsContainer(props: Props) {
  return (
    <div className="mb-10 grid grid-cols-2 gap-4 sm:flex sm:h-22 sm:justify-start sm:gap-8">
      {props.children}
    </div>
  );
}
