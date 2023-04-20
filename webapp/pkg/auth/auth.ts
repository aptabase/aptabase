import { api } from "@app/primitives";
import { trackEvent } from "@aptabase/web";

export type UserAccount = {
  id: string;
  name: string;
  email: string;
  avatarUrl: string;
};

export async function requestSignInLink(email: string): Promise<boolean> {
  const [status] = await api.fetch("POST", "/_auth/signin", { email });

  if (status === 404) return false;
  if (status === 200) return true;

  api.handleError(status);
  return false;
}

export async function requestRegisterLink(name: string, email: string): Promise<void> {
  await api.post("/_auth/register", { name, email });
  trackEvent("register");
}

export async function me(): Promise<UserAccount | null> {
  const [status, account] = await api.fetch<UserAccount>("GET", "/_auth/me");

  if (status === 401) return null;

  return account;
}

export function signOutUrl(): string {
  return `/api/_auth/signout`;
}
