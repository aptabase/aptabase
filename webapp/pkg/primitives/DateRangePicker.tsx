import { Listbox, Transition } from "@headlessui/react";
import { ChevronDownIcon } from "@heroicons/react/20/solid";
import clsx from "clsx";
import { Fragment, useState } from "react";
import { useSearchParams } from "react-router-dom";

type Option = {
  value: string;
  name: string;
};

const options: Option[] = [
  { value: "24h", name: "Last 24 hours" },
  { value: "48h", name: "Last 48 hours" },
  { value: "divider-2", name: "Divider" },
  { value: "7d", name: "Last 7 days" },
  { value: "14d", name: "Last 14 days" },
  { value: "30d", name: "Last 30 days" },
  { value: "divider-1", name: "Divider" },
  { value: "month", name: "This month" },
  { value: "last-month", name: "Last month" },
  { value: "divider-3", name: "Divider" },
  { value: "90d", name: "Last 3 months" },
  { value: "180d", name: "Last 6 months" },
  { value: "365d", name: "Last 12 months" },
  { value: "divider-4", name: "Divider" },
  { value: "all", name: "All time" },
];

const Divider = () => <div className="border-t my-1 border-default" />;

type StyledOptionProps = {
  option: Option;
};

function StyledOption(props: StyledOptionProps) {
  if (props.option.name === "Divider") {
    return <Divider />;
  }

  return (
    <Listbox.Option
      key={props.option.value}
      className={({ active }) =>
        clsx(
          active && "bg-gray-100",
          "relative rounded cursor-default select-none py-1.5 pl-3 pr-9 mx-1"
        )
      }
      value={props.option}
    >
      {() => <span className="block truncate">{props.option?.name}</span>}
    </Listbox.Option>
  );
}

export function DateRangePicker() {
  let [searchParams, setSearchParams] = useSearchParams();
  const period = searchParams.get("period") || "24h";

  const onChange = (option: Option) => {
    setSearchParams((params) => {
      params.set("period", option.value);
      return params;
    });
  };

  const selected =
    options.find((option) => option.value === period) || options[0];

  return (
    <Listbox value={selected} onChange={onChange}>
      {({ open }) => (
        <div className="relative">
          <Listbox.Button className="relative w-full cursor-default tracking-tighter rounded-md py-1.5 pl-3 pr-6 text-default focus-ring hover:bg-gray-100">
            <span className="block truncate">{selected.name}</span>
            <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center">
              <ChevronDownIcon className="h-5 w-5 text-secondary" />
            </span>
          </Listbox.Button>

          <Transition
            show={open}
            as={Fragment}
            leave="transition ease-in duration-100"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <Listbox.Options className="absolute z-10 mt-1 max-h-fit w-50 lg:w-40 right-0 overflow-auto rounded-md bg-white py-1 text-base shadow-lg focus-ring sm:text-sm">
              {options.map((option) => (
                <StyledOption key={option.value} option={option} />
              ))}
            </Listbox.Options>
          </Transition>
        </div>
      )}
    </Listbox>
  );
}
