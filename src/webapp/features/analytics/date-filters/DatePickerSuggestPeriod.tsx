import { trackEvent } from "@aptabase/web";
import { Tooltip } from "@components/Tooltip";
import { DateSuggestion } from "@datepicker-suggest/core";
import { DatePickerSuggest } from "@datepicker-suggest/react";
import { useTheme } from "@features/theme";
import { TooltipContent, TooltipProvider, TooltipTrigger } from "@radix-ui/react-tooltip";
import { IconX } from "@tabler/icons-react";
import { useAtom, useAtomValue, useSetAtom } from "jotai";
import { useEffect, useMemo, useState } from "react";
import {
  clearEndDatePersistentAtom,
  clearStartDatePersistentAtom,
  endDateAtom,
  endDatePersistentLabelAtom,
  periodAtom,
  startDateAtom,
  startDatePersistentLabelAtom,
} from "../../../atoms/date-atoms";

const startOptionsSuggestions = [
  "Today",
  "Yesterday",
  "2 days ago",
  "Last week",
  "Last month",
  "2 months ago",
  "This year",
  "Last year",
];

const endOptionsSuggestions = ["Now"];

type UnappliedDate = {
  end?: DateSuggestion | null;
  start?: DateSuggestion | null;
  startError?: boolean;
  endError?: boolean;
};

export function DatePickerSuggestPeriod() {
  const [startDate, setStartDate] = useAtom(startDateAtom);
  const startDatePersistentLabel = useAtomValue(startDatePersistentLabelAtom);
  const [endDate, setEndDate] = useAtom(endDateAtom);
  const endDatePersistentLabel = useAtomValue(endDatePersistentLabelAtom);
  const clearStartPersistence = useSetAtom(clearStartDatePersistentAtom);
  const clearEndPersistence = useSetAtom(clearEndDatePersistentAtom);
  const [unappliedDate, setUnappliedDate] = useState<UnappliedDate>({});

  const setPeriod = useSetAtom(periodAtom);
  const close = () => {
    setPeriod("24h");

    clearStartPersistence();
    clearEndPersistence();
  };

  const panelColors = usePanelColors();

  const applyEndDate = (newEndDate: DateSuggestion | null) => {
    if (!newEndDate) {
      return;
    }

    if (!startDate || newEndDate.date.getTime() > startDate.date.getTime()) {
      setEndDate(newEndDate);

      if (unappliedDate.start) {
        setStartDate(unappliedDate.start);
        setUnappliedDate({});
      }
    } else {
      setUnappliedDate({ end: newEndDate, startError: true });
    }
  };

  const applyStartDate = (newStartDate: DateSuggestion | null) => {
    if (!newStartDate) {
      return;
    }

    if (!endDate || newStartDate.date.getTime() < endDate.date.getTime()) {
      setStartDate(newStartDate);

      if (unappliedDate.end) {
        setEndDate(unappliedDate.end);
        setUnappliedDate({});
      }
    } else {
      setUnappliedDate({ start: newStartDate, endError: true });
    }
  };

  useEffect(() => {
    trackEvent("date_custom_used");
  }, []);

  return (
    <>
      <div className="flex flex-col md:flex-row gap-2 mt-0.5">
        <div className="flex flex-col">
          <DatePickerSuggest
            className={panelColors + " w-72"}
            onSuggestionChange={(dateSuggestion) => {
              const newStartDate = dateSuggestion ?? startDate;
              applyStartDate(newStartDate);
            }}
            initialSuggestion={startDatePersistentLabel}
            optionsSuggestions={startOptionsSuggestions}
            isError={unappliedDate.startError}
          />
        </div>
        <div className="flex flex-col">
          <DatePickerSuggest
            className={panelColors + " w-72"}
            onSuggestionChange={(dateSuggestion) => {
              const newEndDate = dateSuggestion ?? endDate;
              applyEndDate(newEndDate);
            }}
            initialSuggestion={endDatePersistentLabel}
            optionsSuggestions={endOptionsSuggestions}
            isError={unappliedDate.endError}
          />
        </div>

        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="flex items-end">
                <IconX className="mb-2 cursor-pointer" onClick={close} />
              </div>
            </TooltipTrigger>
            <TooltipContent side="bottom" align="center" className="pt-2">
              <div className="bg-popover text-popover-foreground rounded-md border border-border shadow-md p-2">
                <span className="text-sm">Back to period selection</span>
              </div>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </div>
    </>
  );
}

export function usePanelColors() {
  const { theme } = useTheme();
  return useMemo(() => {
    return theme === "dark" ? "text-white bg-black" : "text-black bg-white";
  }, [theme]);
}
