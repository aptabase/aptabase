import * as React from "react";

import { cn } from "./utils";

export interface InputProps
  extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  description?: string;
  preffix?: string;
}

const TextInput = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, label, description, prefix, ...props }, ref) => {
    return (
      <div>
        {label && (
          <label
            className="text-sm mb-2 block font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            htmlFor={props.id}
          >
            {label}
          </label>
        )}
        <div>
          <div className="flex items-center">
            {prefix && (
              <span className="h-10 items-center flex px-2 text-sm bg-muted border border-r-0 text-muted-foreground">
                {prefix}
              </span>
            )}
            <input
              type={type}
              className={cn(
                "flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
                prefix && "rounded-l-none",
                className
              )}
              ref={ref}
              {...props}
            />
          </div>
        </div>

        {description && (
          <p className="mt-1 text-sm text-muted-foreground">{description}</p>
        )}
      </div>
    );
  }
);

TextInput.displayName = "TextInput";

export { TextInput };
