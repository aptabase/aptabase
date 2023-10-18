type Props = {
  stringProps?: string;
  numericProps?: string;
};

export function EventPropsList(props: Props) {
  const stringProps = JSON.parse(props.stringProps ?? "{}");
  const numericProps = JSON.parse(props.numericProps ?? "{}");
  const eventProps = { ...stringProps, ...numericProps };

  return (
    <div className="text-sm flex mt-1 ml-10">
      {Object.keys(eventProps).map((key) => (
        <div key={key} className="bg-muted text-muted-foreground border py-0.5 px-1 rounded">
          {key}: <span className="text-foreground">{eventProps[key]}</span>
        </div>
      ))}
    </div>
  );
}
