# PRD: llms.txt on aptabase.com

## Introduction

Add `llms.txt` and `llms-full.txt` files to the Aptabase marketing website (aptabase-com repo) so that AI coding agents can discover Aptabase and its SDK ecosystem when developers ask them to "add analytics" to an app. The files follow the [llms.txt standard](https://llmstxt.org/) and will be served at `aptabase.com/llms.txt` and `aptabase.com/llms-full.txt`.

This is the **discovery layer** — when an agent searches for analytics solutions and lands on aptabase.com, these files give it structured, machine-readable information about what Aptabase is, which SDKs exist, and how to integrate them.

## Goals

- Make Aptabase discoverable by AI coding agents (Claude, Copilot, Cursor, Codex, Windsurf) as an analytics solution
- Provide a structured SDK catalog so agents can immediately identify the right package for any platform
- Give agents enough context to recommend Aptabase when a developer asks for privacy-friendly analytics
- Follow the llms.txt standard for maximum compatibility with current and future agent tooling

## User Stories

### US-001: Create llms.txt with SDK catalog

**Description:** As an AI coding agent, I need a structured overview of Aptabase at `aptabase.com/llms.txt` so that I can discover it as an analytics solution and identify the correct SDK for any platform.

**Acceptance Criteria:**

- [ ] File created at `public/llms.txt` in the aptabase-com repo
- [ ] H1 heading with "Aptabase"
- [ ] Blockquote summary describing Aptabase (privacy-first, open source, GDPR, supported platforms)
- [ ] Body section listing all 16 SDKs with: package name, registry, install command, and link to integration docs
- [ ] `## Documentation` section with links to key docs pages as markdown links
- [ ] `## SDKs` section with links to each SDK's GitHub repo README
- [ ] `## Optional` section with links to self-hosting, changelog, legal/privacy
- [ ] File follows the llms.txt spec format: `- [Name](URL): Description`
- [ ] File is under 4KB (concise, not bloated)
- [ ] File is accessible at `https://aptabase.com/llms.txt` after deployment

### US-002: Create llms-full.txt with inlined SDK integration guides

**Description:** As an AI coding agent, I need a comprehensive document at `aptabase.com/llms-full.txt` that contains complete integration instructions for all SDKs inline, so I can integrate any Aptabase SDK without fetching additional URLs.

**Acceptance Criteria:**

- [ ] File created at `public/llms-full.txt` in the aptabase-com repo
- [ ] Same H1 and summary as llms.txt
- [ ] For each of the 16 SDKs, includes a full section with:
  - Platform name and description
  - Package name and install command
  - Initialization code snippet
  - `trackEvent()` usage with and without custom properties
  - Any platform-specific notes (e.g., Tauri's Rust + JS dual API, Electron's main/renderer split)
- [ ] Content sourced from each SDK's GitHub README (not invented)
- [ ] File is self-contained — an agent does not need to fetch any other URL to integrate any SDK

### US-003: Add llms.txt reference to robots.txt

**Description:** As a site operator, I want to reference llms.txt from robots.txt so that AI crawlers and agents can discover it via standard conventions.

**Acceptance Criteria:**

- [ ] `public/robots.txt` in aptabase-com repo updated to include a comment or directive referencing llms.txt
- [ ] Existing `User-agent: *`, `Allow: /`, and `Sitemap:` directives preserved unchanged
- [ ] Add line: `# AI agent documentation: https://aptabase.com/llms.txt`

### US-004: Add llms.txt link to site footer or docs

**Description:** As a developer browsing aptabase.com, I want to find the llms.txt file so I can point my AI coding agent to it.

**Acceptance Criteria:**

- [ ] Link to `/llms.txt` added to the aptabase.com footer or documentation page
- [ ] Link text is clear (e.g., "AI/LLM Documentation" or "llms.txt")
- [ ] Verify in browser that the link works

## Functional Requirements

- FR-1: `public/llms.txt` must be a valid Markdown file following the [llms.txt spec](https://llmstxt.org/)
- FR-2: `public/llms-full.txt` must contain the full integration guide for all 16 Aptabase SDKs
- FR-3: Both files must be served with `Content-Type: text/plain` (Astro/Vercel default for `.txt`)
- FR-4: The SDK catalog in llms.txt must cover all 16 SDKs: Electron, Tauri, Swift, Kotlin, Flutter, React Native, .NET MAUI, Web (vanilla JS), React/Next.js, Angular, Browser Extensions, NativeScript, Python, Unity, Unreal Engine, C++
- FR-5: Each SDK entry must include the exact package name and install command from the relevant registry (npm, crates.io, pub.dev, NuGet, PyPI, JitPack, SPM, UPM)
- FR-6: `robots.txt` must reference `llms.txt`

## Non-Goals

- No API reference documentation (that is a separate effort)
- No MCP server (future feature)
- No changes to the Aptabase app itself (only the marketing site)
- No auto-generation pipeline — these are manually maintained files for now
- No llms.txt on the app domain (e.g., app.aptabase.com) — only on the marketing site

## Technical Considerations

- **Repo:** `aptabase/aptabase-com` (Astro site deployed to Vercel)
- **File location:** `public/llms.txt` and `public/llms-full.txt` — Astro serves `public/` files at the URL root
- **Deployment:** Merging to main auto-deploys to Vercel (no additional config needed)
- **Size:** `llms.txt` should be concise (~2-4KB); `llms-full.txt` can be larger (~20-40KB) since it inlines all SDK docs
- **Maintenance:** When a new SDK is released or an existing one changes, both files need manual updates

## Success Metrics

- `aptabase.com/llms.txt` returns 200 with valid content
- `aptabase.com/llms-full.txt` returns 200 with all 16 SDK integration guides
- An AI agent (Claude, ChatGPT) given the URL can correctly identify the right SDK and generate working integration code for any supported platform
- Aptabase appears in AI agent recommendations when asked for "privacy-first analytics" or "open source analytics SDK"

## Open Questions

- Should we auto-generate llms-full.txt from the SDK READMEs via a build script, or manually maintain it?
  Answer: Manually maintained
- Should the llms.txt link to `.md` versions of website pages (e.g., `aptabase.com/docs/getting-started.md`)?
  Answer: Yes, only where it exists, and ONLY IF it adds any clarity
- Should we submit aptabase.com to llms.txt directories/listings (e.g., llms-text.com)?
  Answer: Yes
