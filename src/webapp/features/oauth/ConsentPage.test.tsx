import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@components/Page", () => ({
  Page: ({ children }: { children: unknown }) => <>{children}</>,
}));
vi.mock("@components/LoadingState", () => ({ LoadingState: () => null }));

import { Component as ConsentPage } from "./ConsentPage";

const redirectUrl = "http://127.0.0.1:53124/callback?code=abc&state=xyz";

const consent = (overrides: object = {}) => ({
  requestId: "req-1",
  client: {
    name: "Stats CLI",
    url: "https://tools.example.com/oauth/client.json",
    host: "tools.example.com",
    verified: false,
  },
  scopes: ["stats:read", "sessions:read"],
  requestedScopes: ["stats:read", "sessions:read"],
  apps: [
    { id: "a1", name: "First App", iconPath: "", hasOwnership: true },
    { id: "a2", name: "Second App", iconPath: "", hasOwnership: false },
  ],
  current: null,
  ...overrides,
});

const json = (status: number, body: unknown) =>
  ({ status, json: async () => body }) as unknown as Response;

let posted: unknown[];
const assign = vi.fn();

function mockFetch(consentBody: unknown) {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (url: string, init?: RequestInit) => {
      if (url === "/api/_oauth/consent" && init?.method === "GET") return json(200, consentBody);
      if (url === "/api/_oauth/consent" && init?.method === "POST") {
        posted.push(JSON.parse(init.body as string));
        return json(200, { redirectUrl });
      }
      if (url === "/api/_auth/me") return json(200, { id: "u1", name: "Jo", email: "jo@example.com", avatarUrl: "" });
      return json(404, {});
    })
  );
}

function renderPage() {
  const client = new QueryClient();
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={["/oauth/consent"]}>
        <ConsentPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

const allowButton = () => screen.getByRole("button", { name: "Allow access" }) as HTMLButtonElement;

describe("ConsentPage", () => {
  beforeEach(() => {
    posted = [];
    assign.mockReset();
    vi.stubGlobal("location", { ...window.location, assign });
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("defaults to selected apps and keeps Allow disabled until an app is ticked", async () => {
    mockFetch(consent());
    renderPage();

    const selected = (await screen.findByRole("radio", { name: "Selected apps" })) as HTMLInputElement;
    const all = screen.getByRole("radio", { name: /All apps/ }) as HTMLInputElement;
    expect(selected.checked).toBe(true);
    expect(all.checked).toBe(false);
    expect(screen.getByText("Includes apps you create or that are shared with you in the future.")).toBeTruthy();

    const first = screen.getByRole("checkbox", { name: /First App/ }) as HTMLInputElement;
    expect(first.checked).toBe(false);
    expect(allowButton().disabled).toBe(true);

    fireEvent.click(first);
    expect(allowButton().disabled).toBe(false);

    // Allow needs a permission as well as an app
    fireEvent.click(screen.getByRole("checkbox", { name: /Read aggregated statistics/ }));
    fireEvent.click(screen.getByRole("checkbox", { name: /Read individual sessions and events/ }));
    expect(allowButton().disabled).toBe(true);
  });

  it("shows the Unverified badge, its warning and the full client URL", async () => {
    mockFetch(consent());
    renderPage();

    expect(await screen.findByText("Stats CLI")).toBeTruthy();
    expect(screen.getByText("https://tools.example.com/oauth/client.json")).toBeTruthy();
    expect(screen.getByText("Unverified")).toBeTruthy();
    expect(screen.queryByText("Verified")).toBeNull();
    expect(
      screen.getByText(
        "Aptabase has not verified who runs this application. Only continue if you started this from an application you trust."
      )
    ).toBeTruthy();
  });

  it("shows the Verified badge without the warning for a verified client", async () => {
    mockFetch(consent({ client: { name: "Stats CLI", url: "https://aptabase.com/cli", host: "aptabase.com", verified: true } }));
    renderPage();

    expect(await screen.findByText("Verified")).toBeTruthy();
    expect(screen.queryByText("Unverified")).toBeNull();
    expect(screen.queryByText(/has not verified/)).toBeNull();
  });

  it("posts the ticked apps and scopes on Allow and follows the redirect", async () => {
    mockFetch(consent());
    renderPage();

    fireEvent.click(await screen.findByRole("checkbox", { name: /Second App/ }));
    fireEvent.click(screen.getByRole("checkbox", { name: /Read individual sessions and events/ }));
    fireEvent.click(allowButton());

    await waitFor(() => expect(assign).toHaveBeenCalledWith(redirectUrl));
    expect(posted).toEqual([{ requestId: "req-1", allow: true, scopes: ["stats:read"], appIds: ["a2"] }]);
  });

  it("posts a denial with no scopes or apps on Deny", async () => {
    mockFetch(consent());
    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Deny" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith(redirectUrl));
    expect(posted).toEqual([{ requestId: "req-1", allow: false, scopes: [], appIds: null }]);
  });

  it("pre-fills from an existing grant", async () => {
    mockFetch(consent({ current: { scopes: ["stats:read"], allApps: false, appIds: ["a1"] } }));
    renderPage();

    const first = (await screen.findByRole("checkbox", { name: /First App/ })) as HTMLInputElement;
    expect(first.checked).toBe(true);
    expect((screen.getByRole("checkbox", { name: /Second App/ }) as HTMLInputElement).checked).toBe(false);
    expect(allowButton().disabled).toBe(false);
  });
});
