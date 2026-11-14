import { api } from "@fns/api";

// The query API endpoints answer errors as { error, message } rather than the
// validation shape that api.handleError expects, and answer 404 when the feature
// is switched off on the server. These helpers cover both.

export class RequestError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

const fallbackMessage = "Something went wrong. Please try again.";

async function toError(status: number, response: Response): Promise<RequestError> {
  try {
    const body = (await response.json()) as { error?: unknown; message?: unknown };
    return new RequestError(
      status,
      typeof body.error === "string" ? body.error : "",
      typeof body.message === "string" && body.message ? body.message : fallbackMessage
    );
  } catch {
    return new RequestError(status, "", fallbackMessage);
  }
}

export async function request<T>(method: "GET" | "POST" | "DELETE", path: string, body?: unknown): Promise<T> {
  const [status, response] = await api.fetch(method, path, body);
  if (status !== 200) throw await toError(status, response);

  return (await response.json()) as T;
}

// Resolves to null when the feature is off or the caller may not see the resource
export async function getOptional<T>(path: string): Promise<T | null> {
  const [status, response] = await api.fetch("GET", path);
  if (status === 403 || status === 404) return null;
  if (status !== 200) throw await toError(status, response);

  return (await response.json()) as T;
}

export function errorMessage(e: unknown): string {
  return e instanceof RequestError ? e.message : fallbackMessage;
}
