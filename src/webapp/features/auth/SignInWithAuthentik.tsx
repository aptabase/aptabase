import { Button } from "@components/Button";

export function SignInWithAuthentik() {
  const url = `/api/_auth/authentik`;

  return (
    <Button asChild variant="outline">
      <a href={url} className="flex space-x-2 w-full">
        <svg xmlns="http://www.w3.org/2000/svg" className="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
          <path
            d="M13.96 9.01h-0.84V7.492h-1.234v3.663H5.722c0.34 0.517 0.538 0.982 0.538 1.152 0 0.46 -1.445 3.059 -3.197 3.059C0.8 15.427 -0.745 12.8 0.372 10.855a3.062 3.062 0 0 1 2.691 -1.606c1.04 0 1.971 0.915 2.557 1.755V6.577a3.773 3.773 0 0 1 3.77 -3.769h10.84C22.31 2.808 24 4.5 24 6.577v10.845a3.773 3.773 0 0 1 -3.77 3.769h-1.6V17.5h-7.64v3.692h-1.6a3.773 3.773 0 0 1 -3.77 -3.769v-3.41h12.114v-6.52h-1.59v0.893h-0.84v-0.893H13.96v1.516Zm-9.956 1.845c-0.662 -0.703 -1.578 -0.544 -2.209 0 -2.105 2.054 1.338 5.553 3.302 1.447a5.395 5.395 0 0 0 -1.093 -1.447Z"
            stroke-width="1"
          ></path>
        </svg>
        <span>Continue with Authentik</span>
      </a>
    </Button>
  );
}
