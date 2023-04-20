function handleError(status: number) {
  if (status >= 500) {
    // TODO: show error toast
  } else if (status === 401) {
    // TODO: show error toast
  } else if (status === 403) {
    // TODO: show error toast
  }
}

const commonHeaders: RequestInit = {
  credentials: "include",
};

const mutatingHeaders: RequestInit = {
  ...commonHeaders,
  headers: {
    Accept: "application/json",
    "Content-Type": "application/json",
  },
};

async function _fetch<T>(method: string, path: string, body?: any): Promise<[number, T]> {
  const response = await window.fetch(`/api${path}`, {
    ...(method === "GET" ? commonHeaders : mutatingHeaders),
    method,
    body: body ? JSON.stringify(body) : undefined,
  });

  const responseBody = response.json() as T;
  return [response.status, responseBody];
}

async function get<T>(path: string, params?: Record<string, any>): Promise<T> {
  const paramsStr = params ? new URLSearchParams(params).toString() : "";
  const pathWithParams = paramsStr ? `${path}?${paramsStr}` : path;

  const [status, response] = await _fetch<T>("GET", pathWithParams);

  if (status >= 400) {
    handleError(status);
  }

  return response;
}

async function post<T>(path: string, body: any): Promise<T> {
  const [status, response] = await _fetch<T>("POST", path, body);

  if (status >= 400) {
    handleError(status);
  }

  return response;
}

async function put<T>(path: string, body: any): Promise<T> {
  const [status, response] = await _fetch<T>("PUT", path, body);

  if (status >= 400) {
    handleError(status);
  }

  return response;
}

async function _delete<T>(path: string): Promise<T> {
  const [status, response] = await _fetch<T>("DELETE", path);

  if (status >= 400) {
    handleError(status);
  }

  return response;
}

export const api = { fetch: _fetch, post, put, delete: _delete, get, handleError };
