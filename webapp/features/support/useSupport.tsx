import { useAuth } from "../auth/AuthProvider";

type SupportFunctions = {
  toggleChat: () => void;
};

export function useSupport(): SupportFunctions {
  const user = useAuth();

  const toggleChat = () => {
    window.$crisp?.push(["set", "user:nickname", [user.name]]);
    window.$crisp?.push(["set", "user:email", [user.email]]);
    window.$crisp?.push(["set", "user:avatar", [user.avatarUrl]]);
    window.$crisp?.push(["do", "chat:show"]);
    window.$crisp?.push(["do", "chat:open"]);
  };

  return { toggleChat };
}
