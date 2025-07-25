import { trackEvent } from "@aptabase/web";
import { api } from "@fns/api";

export type UserAccount = {
  id: string;
  name: string;
  email: string;
  avatarUrl: string;
};

export async function requestSignInLink(email: string): Promise<boolean | string> {
  const [status, response] = await api.fetch("POST", "/_auth/signin", {
    email,
  });

  if (status === 404) return false;
  if (status === 200) return true;
  if (status === 429) {
    return "Too many requests. Please wait a moment before trying again.";
  }
  if (status === 400) {
    const errors = (await response.json()) as { errors?: { email?: string[] } };
    if (errors.errors?.email?.length) {
      return errors.errors.email[0];
    }
  }

  await api.handleError(status, response);
  return false;
}

export async function requestRegisterLink(name: string, email: string): Promise<string | void> {
  const [status, response] = await api.fetch("POST", "/_auth/register", { name, email });
  if (status === 200) {
    trackEvent("register");
    return;
  }
  if (status === 429) {
    return "Too many requests. Please wait a moment before trying again.";
  }
  if (status === 400) {
    const errors = (await response.json()) as { errors?: { email?: string[] } };
    if (errors.errors?.email?.length) {
      return errors.errors.email[0];
    }
  }
  await api.handleError(status, response);
}

export async function me(): Promise<UserAccount | null> {
  const [status, account] = await api.fetch("GET", "/_auth/me");

  if (status === 401) return null;

  return account.json() as Promise<UserAccount | null>;
}

export async function signOut(): Promise<void> {
  await api.fetch("POST", "/_auth/signout");
  location.href = "/auth";
}
