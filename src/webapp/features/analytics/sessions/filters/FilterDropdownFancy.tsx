import {
  Badge,
  Button,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@components/FancyBox";
import { TopNQueryChildrenProps } from "@features/analytics/query";
import { IconChevronDown } from "@tabler/icons-react";
import { useMemo, useState } from "react";

type Props = {
  value: string | undefined;
  placeholder?: string;
  onValueChange: (value: string | undefined) => void;
  data: TopNQueryChildrenProps;
};

export function FilterDropdownFancy(props: Props) {
  const [open, setOpen] = useState(false);

  if (!props.data.items?.length) {
    return null;
  }

  const options = useMemo(
    () => props.data.items?.filter((i) => !!i.name).sort((a, b) => a.name.localeCompare(b.name)),
    [props.data.items]
  );

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" role="combobox" aria-expanded={open} className="w-48 justify-between">
          {props.value ? (
            <Badge variant="outline" className="mr-2">
              {props.value}
            </Badge>
          ) : (
            props.placeholder
          )}
          <IconChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-48 p-0" align="start">
        <Command className="rounded-lg border shadow-md">
          <CommandInput placeholder={`Search ${props.placeholder?.toLowerCase()}...`} className="h-9" />
          <CommandEmpty>No results found.</CommandEmpty>
          <CommandGroup className="max-h-[200px] overflow-auto">
            {options.map((option) => (
              <CommandItem
                key={option.value}
                value={option.name}
                onSelect={(value) => {
                  props.onValueChange(value);
                  setOpen(false);
                }}
                className="px-2 py-1.5"
              >
                {option.name || "Unknown"}
              </CommandItem>
            ))}
          </CommandGroup>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
