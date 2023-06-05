import { signOutUrl, UserAccount, UserAvatar } from "@app/auth";
import { RegionFlag } from "@app/primitives";
import { Menu, Transition } from "@headlessui/react";
import clsx from "clsx";
import { Fragment } from "react";

type Props = {
  user: UserAccount;
};

const nav = [{ name: "Sign out", href: signOutUrl() }];

const Divider = () => <div className="border-t my-1 border-default" />;

export function UserMenu(props: Props) {
  return (
    <Menu as="div" className="relative">
      <div>
        <Menu.Button className="flex max-w-xs px-2 py-1 items-center rounded text-sm focus-ring">
          <UserAvatar user={props.user} />
          <div className="hidden lg:block ml-2 text-default">
            {props.user.name}
          </div>
        </Menu.Button>
      </div>
      <Transition
        as={Fragment}
        enter="transition ease-out duration-100"
        enterFrom="transform opacity-0 scale-95"
        enterTo="transform opacity-100 scale-100"
        leave="transition ease-in duration-75"
        leaveFrom="transform opacity-100 scale-100"
        leaveTo="transform opacity-0 scale-95"
      >
        <Menu.Items className="absolute right-0 lg:bottom-8 lg:right-auto z-10 mt-2 w-60 origin-top-right rounded-md py-1 shadow-lg border bg-default border-default focus-ring">
          <div className="px-3 py-1 text-xs flex items-center justify-between">
            <div>
              <span className="text-subtle">Signed in as</span>
              <span className="block truncate text-sm font-medium text-default">
                {props.user.email}
              </span>
            </div>
            <RegionFlag />
          </div>
          <Divider />
          {nav.map((item) => (
            <Menu.Item key={item.name}>
              {({ active }) => (
                <a
                  href={item.href}
                  className={clsx(
                    active ? "bg-subtle text-inverted" : "",
                    "block mx-1 rounded px-4 py-2 text-sm text-default"
                  )}
                >
                  {item.name}
                </a>
              )}
            </Menu.Item>
          ))}
        </Menu.Items>
      </Transition>
    </Menu>
  );
}
