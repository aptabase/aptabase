import { IconCode, TablerIconsProps } from "@tabler/icons-react";
import { twMerge } from "tailwind-merge";

type ToolProps = {
  icon: string | ((props: TablerIconsProps) => JSX.Element);
  iconClassName?: string;
  name: string;
  href: string;
  description: string;
};

function iconUrl(name: string): string | undefined {
  const svg = new URL(`./icons/${name}.svg`, import.meta.url);
  if (svg.href.endsWith("/undefined")) {
    return undefined;
  }

  return svg.href;
}

function Tool(props: ToolProps) {
  return (
    <a
      href={props.href}
      className="flex border p-4 rounded-lg bg-card w-full hover:bg-muted"
      target="_blank"
      rel="noopener noreferrer"
    >
      {typeof props.icon === "string" ? (
        <img src={iconUrl(props.icon)} className={twMerge("h-6 w-6 mt-1", props.iconClassName)} />
      ) : (
        <props.icon className={twMerge("h-6 w-6 mt-1", props.iconClassName)} />
      )}
      <div className="flex flex-col ml-4">
        <span>{props.name}</span>
        <p className="text-sm text-muted-foreground">{props.description}</p>
      </div>
    </a>
  );
}

export function ToolsList() {
  return (
    <>
      <p className="text-sm mb-2">Here is a list of some free tools you can use to query your data.</p>
      <div className="space-y-2 max-w-3xl">
        <Tool
          icon="excel"
          name="Microsoft Excel"
          href="https://www.microsoft.com/en-ie/microsoft-365"
          description="The most popular spreadsheet works great with Aptabase CSV files. Open it on Excel and start exploring your data with Pivot Tables and Charts."
        />

        <Tool
          icon="powerbi"
          name="Microsoft Power BI"
          href="https://powerbi.microsoft.com/en-us/"
          description="Power BI is another alternative worth considering. It's a powerful tool for data visualization and analysis. It takes longer to learn than Excel, but it's also a lot more powerful."
        />

        <Tool
          icon="jupyter"
          name="JupyterLab and Jupyter Notebook"
          href="https://jupyter.org/"
          description="Jupyter is a flexible tool for data analysis and visualization with support for over 40 programming languages, including Python and R."
        />

        <Tool
          icon={IconCode}
          name="Build your own pipeline"
          href="https://google.com"
          description="CSV is a widely supported format that most visualization tools and programming languages support natively. You can use any tool you like to explore your data."
        />
      </div>
    </>
  );
}
