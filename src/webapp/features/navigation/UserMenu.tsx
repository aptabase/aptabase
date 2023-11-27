import React, { Fragment } from "react";
import { Menu, Transition } from "@headlessui/react";
import { IconCreditCard, IconDoorExit } from "@tabler/icons-react";
import { Link } from "react-router-dom";
import { useBillingState } from "@features/billing";
import { isBillingEnabled } from "@features/env";
import { signOut, UserAccount, UserAvatar } from "../auth";
import { twMerge } from "tailwind-merge";
import { PingSignal } from "@components/PingSignal";
import { ThemeToggle } from "@features/theme";

type Props = {
  user: UserAccount;
};

const Divider = () => <div className="border-t my-1" />;

const MenuItem = (props: {
  href: string;
  onClick?: () => Promise<void> | void;
  reloadDocument?: boolean;
  children: React.ReactNode;
}) => (
  <Menu.Item>
    {({ active }) => (
      <Link
        to={props.href}
        onClick={props.onClick}
        reloadDocument={props.reloadDocument}
        className={twMerge(
          "flex mx-1 rounded p-2 text-sm items-center space-x-2",
          active ? "bg-accent text-foreground" : ""
        )}
      >
        {props.children}
      </Link>
    )}
  </Menu.Item>
);

export function UserMenu(props: Props) {
  const billing = useBillingState();

  return (
    <Menu as="div" className="relative">
      <Menu.Button className="flex w-full p-2 gap-2 items-center rounded text-sm focus-ring hover:bg-accent">
        {({ open }) => (
          <>
            <UserAvatar user={props.user} />
            <div className="hidden lg:block">{props.user.name}</div>
            {!open && billing === "OVERUSE" && <PingSignal color="warning" size="sm" />}
          </>
        )}
      </Menu.Button>
      <Transition
        as={Fragment}
        enter="transition ease-out duration-100"
        enterFrom="transform opacity-0 scale-95"
        enterTo="transform opacity-100 scale-100"
        leave="transition ease-in duration-75"
        leaveFrom="transform opacity-100 scale-100"
        leaveTo="transform opacity-0 scale-95"
      >
        <Menu.Items className="absolute right-0 lg:bottom-8 lg:right-auto z-10 mt-2 w-60 origin-top-right rounded-md py-1 shadow-lg border bg-background focus-ring">
          <div className="px-3 py-1 text-xs flex items-center justify-between">
            <div>
              <span className="text-muted-foreground">Signed in as</span>
              <span className="block truncate text-sm font-medium">{props.user.email}</span>
            </div>
            <ThemeToggle />
          </div>
          {isBillingEnabled && (
            <>
              <Divider />
              <MenuItem href="/billing">
                <IconCreditCard className="w-4 h-4" />
                <span>Billing</span>
                {billing === "OVERUSE" && <PingSignal color="warning" size="sm" />}
              </MenuItem>
            </>
          )}
          <Divider />
          <MenuItem href="#" onClick={signOut}>
            <IconDoorExit className="w-4 h-4" />
            <span>Sign out</span>
          </MenuItem>
        </Menu.Items>
      </Transition>
    </Menu>
  );
}
