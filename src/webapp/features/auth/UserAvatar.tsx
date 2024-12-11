import { useState } from "react";
import { UserAccount } from ".";

type Props = {
  user: UserAccount;
};

export function UserAvatar(props: Props) {
  const [errorRetryCount, setErrorRetryCount] = useState(0);
  const handleError = (e: React.SyntheticEvent<HTMLImageElement, Event>) => {
    const parts = props.user.name.split(" ");
    let first = parts.shift()?.charAt(0) ?? "";
    let last = parts.pop()?.charAt(0) ?? "";

    // If there is only one name, add a space at the start to make the second letter smaller
    const initials = last ? `${first}${last}` : ` ${first}`;
    if (errorRetryCount <= 1) {
      e.currentTarget.src = `https://avatar.vercel.sh/${props.user.id}.svg?text=${initials}`;
    } else {
      e.currentTarget.src = "";
    }
    setErrorRetryCount(errorRetryCount + 1);
  };

  return (
    <img
      onError={handleError}
      className="h-8 w-8 lg:h-6 lg:w-6 rounded-full"
      loading="lazy"
      src={props.user.avatarUrl}
      alt={props.user.name}
    />
  );
}
