import { ComponentPropsWithoutRef, ElementRef, forwardRef } from "react";
import { twMerge } from "tailwind-merge";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "./Accordion";
import { Badge } from "./Badge";
import { Button } from "./Button";
import { Command, CommandDialog, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "./Command";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "./Dialog";
import { Label } from "./Label";
import { Popover, PopoverContent, PopoverTrigger } from "./Popover";
import { TextInput } from "./TextInput";

interface FancyBoxProps extends ComponentPropsWithoutRef<"div"> {
  variant?: "default" | "secondary";
}

export const FancyBox = forwardRef<ElementRef<"div">, FancyBoxProps>(
  ({ className, variant = "default", ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={twMerge(
          "relative flex flex-col gap-4 rounded-lg border p-4",
          variant === "secondary" && "bg-muted/50",
          className
        )}
        {...props}
      />
    );
  }
);
FancyBox.displayName = "FancyBox";

interface FancyBoxHeaderProps extends ComponentPropsWithoutRef<"div"> {}

export const FancyBoxHeader = forwardRef<ElementRef<"div">, FancyBoxHeaderProps>(({ className, ...props }, ref) => {
  return <div ref={ref} className={twMerge("flex items-center justify-between gap-4", className)} {...props} />;
});
FancyBoxHeader.displayName = "FancyBoxHeader";

interface FancyBoxTitleProps extends ComponentPropsWithoutRef<"h3"> {}

export const FancyBoxTitle = forwardRef<ElementRef<"h3">, FancyBoxTitleProps>(({ className, ...props }, ref) => {
  return (
    <h3 ref={ref} className={twMerge("text-xl font-semibold leading-none tracking-tight", className)} {...props} />
  );
});
FancyBoxTitle.displayName = "FancyBoxTitle";

interface FancyBoxDescriptionProps extends ComponentPropsWithoutRef<"p"> {}

export const FancyBoxDescription = forwardRef<ElementRef<"p">, FancyBoxDescriptionProps>(
  ({ className, ...props }, ref) => {
    return <p ref={ref} className={twMerge("text-sm text-muted-foreground", className)} {...props} />;
  }
);
FancyBoxDescription.displayName = "FancyBoxDescription";

interface FancyBoxContentProps extends ComponentPropsWithoutRef<"div"> {}

export const FancyBoxContent = forwardRef<ElementRef<"div">, FancyBoxContentProps>(({ className, ...props }, ref) => {
  return <div ref={ref} className={twMerge("flex flex-col gap-4", className)} {...props} />;
});
FancyBoxContent.displayName = "FancyBoxContent";

interface FancyBoxFooterProps extends ComponentPropsWithoutRef<"div"> {}

export const FancyBoxFooter = forwardRef<ElementRef<"div">, FancyBoxFooterProps>(({ className, ...props }, ref) => {
  return <div ref={ref} className={twMerge("flex items-center gap-2", className)} {...props} />;
});
FancyBoxFooter.displayName = "FancyBoxFooter";

export {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
  Badge,
  Button,
  Command,
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Label,
  Popover,
  PopoverContent,
  PopoverTrigger,
  TextInput,
};
