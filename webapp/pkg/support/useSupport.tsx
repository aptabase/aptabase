type SupportFunctions = {
  toggleChat: () => void;
};

export function useSupport(): SupportFunctions {
  const toggleChat = () => {
    window.$crisp?.push(["do", "chat:show"]);
    window.$crisp?.push(["do", "chat:open"]);
  };

  return { toggleChat };
}
