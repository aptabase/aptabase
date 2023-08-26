import clsx from "clsx";
import { RadioGroup } from "@headlessui/react";
import { IconBug, IconRocket } from "@tabler/icons-react";
import { useApps } from "@features/apps";

const options = [
  { icon: IconRocket, label: "Release", value: "release" },
  { icon: IconBug, label: "Debug", value: "debug" },
];

export function BuildModeSelector() {
  const { buildMode, switchBuildMode } = useApps();

  return (
    <div className="p-2 text-sm space-y-2">
      <p className="whitespace-nowrap">What data source do you want see?</p>
      <RadioGroup value={buildMode} onChange={switchBuildMode}>
        <div className="grid grid-cols-2">
          {options.map((option) => (
            <RadioGroup.Option
              key={option.value}
              value={option.value}
              className={({ checked }) =>
                clsx(
                  checked ? "bg-accent" : "text-muted-foreground",
                  "flex cursor-pointer items-center justify-center rounded p-2 focus-ring"
                )
              }
            >
              <RadioGroup.Label as="span" className="flex items-center space-x-2">
                <option.icon className="h-5 w-5" stroke="1.5" />
                <span>{option.label}</span>
              </RadioGroup.Label>
            </RadioGroup.Option>
          ))}
        </div>
      </RadioGroup>
      <p className="text-muted-foreground text-xs text-center">
        Your data is separated by{" "}
        <a
          target="_blank"
          className="underline hover:text-white"
          href="https://aptabase.com/docs/build-modes"
        >
          build mode
        </a>
        .
      </p>
    </div>
  );
}
