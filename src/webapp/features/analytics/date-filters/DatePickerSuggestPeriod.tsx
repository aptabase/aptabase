import { trackEvent } from "@aptabase/web";
import { Tooltip } from "@components/Tooltip";
import { DatePickerSuggest } from "@datepicker-suggest/react";
import { useTheme } from "@features/theme";
import { TooltipContent, TooltipProvider, TooltipTrigger } from "@radix-ui/react-tooltip";
import { IconX } from "@tabler/icons-react";
import { useAtom, useAtomValue, useSetAtom } from "jotai";
import { useEffect, useMemo } from "react";
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

export function DatePickerSuggestPeriod() {
  const [startDate, setStartDate] = useAtom(startDateAtom);
  const startDatePersistentLabel = useAtomValue(startDatePersistentLabelAtom);
  const [endDate, setEndDate] = useAtom(endDateAtom);
  const endDatePersistentLabel = useAtomValue(endDatePersistentLabelAtom);
  const clearStartPersistence = useSetAtom(clearStartDatePersistentAtom);
  const clearEndPersistence = useSetAtom(clearEndDatePersistentAtom);

  const setPeriod = useSetAtom(periodAtom);
  const close = () => {
    setPeriod("24h");

    clearStartPersistence();
    clearEndPersistence();
  };

  const panelColors = usePanelColors();

  useEffect(() => {
    trackEvent("date_custom_used");
  }, []);

  useEffect(() => {
    if (!endDate || !startDate) {
      return;
    }
    if (endDate.date.getTime() < startDate.date.getTime()) {
      setStartDate(endDate);
    }
  }, [endDate]);

  useEffect(() => {
    if (!startDate || !endDate) {
      return;
    }
    if (startDate.date.getTime() > endDate.date.getTime()) {
      setEndDate(startDate);
    }
  }, [startDate]);

  return (
    <>
      <div className="flex flex-col md:flex-row gap-2 mt-0.5">
        <div className="flex flex-col">
          <DatePickerSuggest
            panelClassName={panelColors + " w-72"}
            onSuggestionChange={(dateSuggestion) => {
              const newStartDate = dateSuggestion ?? startDate;
              setStartDate(newStartDate);
            }}
            initialSuggestion={startDatePersistentLabel}
            optionsSuggestions={startOptionsSuggestions}
          />
        </div>
        <div className="flex flex-col">
          <DatePickerSuggest
            panelClassName={panelColors + " w-72"}
            onSuggestionChange={(dateSuggestion) => {
              const newEndDate = dateSuggestion ?? endDate;
              setEndDate(newEndDate);
            }}
            initialSuggestion={endDatePersistentLabel}
            optionsSuggestions={endOptionsSuggestions}
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
