import { Button } from "@components/Button";
import { Command, CommandGroup, CommandInput, CommandItem } from "@components/Command";
import { Popover, PopoverContent, PopoverTrigger } from "@components/Popover";
import { TopNQueryChildrenProps } from "@features/analytics/query";
import { IconChevronDown, IconPlus, IconX } from "@tabler/icons-react";
import { useMemo, useState } from "react";

type Props = {
  value: string | undefined;
  placeholder?: string;
  onValueChange: (value: string | undefined) => void;
  data: TopNQueryChildrenProps;
};

export function FilterDropdownFancy(props: Props) {
  const [open, setOpen] = useState(false);
  const [inputValue, setInputValue] = useState("");

  const options = useMemo(
    () => props.data.items?.filter((i) => !!i.name).sort((a, b) => a.name.localeCompare(b.name)),
    [props.data.items]
  );

  if (!props.data.items?.length && !inputValue) {
    return null;
  }

  const filteredOptions = options
    ?.filter((item) => item.name.toLowerCase().includes(inputValue.toLowerCase()))
    .sort((a, b) => a.name.localeCompare(b.name));

  const handleSelect = (value: string) => {
    props.onValueChange(value);
    setOpen(false);
    setInputValue("");
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    props.onValueChange(undefined);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" role="combobox" aria-expanded={open} className="w-48 justify-between">
          <div className="flex flex-1 items-center gap-2 truncate">
            {props.value ? (
              <>
                <span className="truncate">{props.value}</span>
                <IconX className="h-4 w-4 shrink-0 opacity-50 hover:opacity-100 ml-auto" onClick={handleClear} />
              </>
            ) : (
              props.placeholder
            )}
          </div>
          <IconChevronDown className="h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-48 p-0" align="start">
        <Command className="rounded-lg border shadow-md [&_[cmdk-input-wrapper]_svg]:!hidden">
          <div className="flex items-center px-1 border-b">
            <IconPlus className="h-4 w-4 shrink-0 opacity-50" />
            <CommandInput
              placeholder="Type a custom name..."
              value={inputValue}
              onValueChange={setInputValue}
              className="h-9 border-0"
            />
          </div>
          <CommandGroup className="max-h-[200px] overflow-auto">
            {filteredOptions?.map((option) => (
              <CommandItem
                key={option.value}
                value={option.name}
                onSelect={() => handleSelect(option.name)}
                className="px-2 py-1.5"
              >
                {option.name || "Unknown"}
              </CommandItem>
            ))}
            {inputValue && (!filteredOptions || filteredOptions.length === 0) && (
              <CommandItem
                value={inputValue}
                onSelect={() => handleSelect(inputValue)}
                className="px-2 py-1.5 flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
              >
                <IconPlus className="h-4 w-4" />
                Filter by: "{inputValue}"
              </CommandItem>
            )}
          </CommandGroup>
        </Command>
      </PopoverContent>
    </Popover>
  );
}
