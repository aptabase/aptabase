type Props = {
  label: string;
  value: string;
};

export function Metric(props: Props) {
  return (
    <div className="flex flex-col pl-4">
      <div className="text-2xl font-semibold">{props.value}</div>
      <div className="text-sm text-secondary">{props.label}</div>
    </div>
  );
}
