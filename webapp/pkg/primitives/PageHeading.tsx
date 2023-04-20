type Props = {
  title: string;
  subtitle?: string;
};

export function PageHeading(props: Props) {
  return (
    <div>
      <h1 className="text-2xl font-medium">{props.title}</h1>
      {props.subtitle && <p className="text-secondary">{props.subtitle}</p>}
    </div>
  );
}
