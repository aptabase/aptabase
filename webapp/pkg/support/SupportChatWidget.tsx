import { isSupportEnabled } from "@app/env";

export function SupportChatWidget() {
  if (!isSupportEnabled) return null;

  return (
    <script
      type="text/javascript"
      dangerouslySetInnerHTML={{
        __html: `
        window.$crisp = [];
        window.$crisp.push(["do", "chat:hide"]);
        window.CRISP_WEBSITE_ID = "6a643f4e-c28f-44df-91af-8072159892ea";
  
        document.addEventListener("DOMContentLoaded", function () {
          setTimeout(() => {
            d = document;
            s = d.createElement("script");
            s.src = "https://client.crisp.chat/l.js";
            s.async = 1;
            d.getElementsByTagName("head")[0].appendChild(s);
          }, 3000);
        });
        `,
      }}
    ></script>
  );
}
