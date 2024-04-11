type Props = {
  title?: string;
  children: React.ReactNode;
};

export function NavCategory(props: Props) {
  return (
    <div>
      {props.title && (
        <span className="text-xs tracking-tight font-medium ml-2 leading-6 text-muted-foreground">{props.title}</span>
      )}
      <nav className="space-y-0.5">{props.children}</nav>
    </div>
  );
}
