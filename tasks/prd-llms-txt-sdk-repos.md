# PRD: llms.txt in Aptabase SDK Repos

## Introduction

Add `llms.txt` files to each of the 16 Aptabase SDK GitHub repositories so that AI coding agents have a structured, agent-optimized integration guide available directly in the repo. When an agent discovers an Aptabase SDK (via npm, pub.dev, crates.io, etc.) and navigates to the GitHub repo, the `llms.txt` file gives it everything needed to integrate the SDK — install commands, initialization, event tracking, configuration options, and platform-specific notes.

This is the **integration layer** — once an agent has found the right SDK, these files ensure it can generate correct, working integration code.

## Goals

- Provide every Aptabase SDK repo with a structured `llms.txt` optimized for AI agent consumption
- Ensure agents can integrate any Aptabase SDK correctly without needing to parse HTML docs or guess at APIs
- Standardize the information available across all SDKs (consistent structure, same sections)
- Cover all 16 SDKs across Desktop, Mobile, Web, Game Engines, and Backend platforms

## User Stories

### US-001: Create llms.txt template for SDK repos

**Description:** As a maintainer, I need a standardized llms.txt template so that all SDK repos have consistent, agent-friendly documentation.

**Acceptance Criteria:**

- [ ] Template markdown file created with standard sections: Overview, Installation, Initialization, Track Events, Track Events with Properties, Configuration, Platform Notes
- [ ] Template includes placeholder variables for SDK-specific values (package name, install command, import path, init function, etc.)
- [ ] Template follows the llms.txt spec format
- [ ] Template stored in `tasks/templates/sdk-llms-txt-template.md` for reference

### US-002: Add llms.txt to aptabase-electron

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-electron repo so I can integrate Aptabase into an Electron app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-electron`
- [ ] Contains: package name (`@aptabase/electron`), npm install command
- [ ] Documents main process initialization: `initialize("<APP_KEY>")` from `@aptabase/electron/main`
- [ ] Documents renderer process tracking: `trackEvent(name, props?)` from `@aptabase/electron/renderer`
- [ ] Includes code examples for both main and renderer process
- [ ] Mentions the main/renderer process split clearly

### US-003: Add llms.txt to tauri-plugin-aptabase

**Description:** As an AI coding agent, I need `llms.txt` in the tauri-plugin-aptabase repo so I can integrate Aptabase into a Tauri app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/tauri-plugin-aptabase`
- [ ] Documents dual installation: Rust crate (`cargo add tauri-plugin-aptabase`) AND npm package (`npm add @aptabase/tauri`)
- [ ] Documents Rust API: `EventTracker` trait, `track_event()` on `App`/`AppHandle`/`Window`
- [ ] Documents JavaScript API: `trackEvent()` from `@aptabase/tauri`
- [ ] Mentions ACL permission requirement: `aptabase:allow-track-event`
- [ ] Includes code examples for both Rust and JS usage

### US-004: Add llms.txt to aptabase-swift

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-swift repo so I can integrate Aptabase into a Swift/iOS/macOS app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-swift`
- [ ] Documents three install methods: SPM (package URL), Xcode (Add Package), CocoaPods
- [ ] Documents initialization: `Aptabase.shared.initialize(appKey:)` in AppDelegate or @main
- [ ] Documents tracking: `Aptabase.shared.trackEvent(name:, with:)`
- [ ] Lists supported platforms: iOS, macOS, watchOS, tvOS

### US-005: Add llms.txt to aptabase-kotlin

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-kotlin repo so I can integrate Aptabase into an Android/Kotlin app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-kotlin`
- [ ] Documents JitPack setup: add JitPack repository to `settings.gradle.kts`
- [ ] Documents Gradle dependency: `implementation("com.github.aptabase:aptabase-kotlin:VERSION")`
- [ ] Documents initialization and `trackEvent()` API
- [ ] Mentions Android context requirement

### US-006: Add llms.txt to aptabase_flutter

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase_flutter repo so I can integrate Aptabase into a Flutter app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase_flutter`
- [ ] Documents install: `flutter pub add aptabase_flutter`
- [ ] Documents initialization: `Aptabase.init("<APP_KEY>")` in `main()`
- [ ] Documents tracking: `Aptabase.instance.trackEvent(name, props?)`
- [ ] Lists supported platforms: iOS, Android, Desktop, Web

### US-007: Add llms.txt to aptabase-react-native

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-react-native repo so I can integrate Aptabase into a React Native app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-react-native`
- [ ] Documents install: `npm add @aptabase/react-native`
- [ ] Documents initialization and `trackEvent()` usage
- [ ] Includes code example with the React Native context/provider pattern if applicable

### US-008: Add llms.txt to aptabase-maui

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-maui repo so I can integrate Aptabase into a .NET MAUI app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-maui`
- [ ] Documents install: `dotnet add package Aptabase.Maui`
- [ ] Documents `MauiProgram.cs` setup with `UseAptabase("<APP_KEY>")`
- [ ] Documents tracking via dependency injection or static API

### US-009: Add llms.txt to aptabase-js (web, react, angular, browser)

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-js monorepo so I can integrate Aptabase into Web, React/Next.js, Angular, or Browser Extension projects.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-js`
- [ ] Documents all 4 packages: `@aptabase/web`, `@aptabase/react`, `@aptabase/angular`, `@aptabase/browser`
- [ ] For `@aptabase/react`: includes Next.js App Router and Pages Router setup
- [ ] For `@aptabase/angular`: includes module/service setup
- [ ] For `@aptabase/browser`: includes manifest.json permission if needed
- [ ] Each package has its own section with install command, initialization, and tracking example

### US-010: Add llms.txt to aptabase-python

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-python repo so I can integrate Aptabase into a Python app.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-python`
- [ ] Documents install: `pip install aptabase`
- [ ] Documents initialization and `track_event()` usage
- [ ] Includes code example

### US-011: Add llms.txt to aptabase-unity

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-unity repo so I can integrate Aptabase into a Unity project.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-unity`
- [ ] Documents Unity Package Manager installation via git URL
- [ ] Documents initialization and tracking API in C#
- [ ] Includes code example for MonoBehaviour usage

### US-012: Add llms.txt to aptabase-unreal

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-unreal repo so I can integrate Aptabase into an Unreal Engine 5 project.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-unreal`
- [ ] Documents plugin installation (clone into Plugins folder)
- [ ] Documents C++ and/or Blueprint API for tracking events
- [ ] Includes .uplugin or Build.cs dependency notes if applicable

### US-013: Add llms.txt to aptabase-cpp

**Description:** As an AI coding agent, I need `llms.txt` in the aptabase-cpp repo so I can integrate Aptabase into a C++ project.

**Acceptance Criteria:**

- [ ] `llms.txt` created at repo root of `aptabase/aptabase-cpp`
- [ ] Documents CMake integration (`add_subdirectory`)
- [ ] Documents networking backend options (cpp-httplib vs Boost.Asio)
- [ ] Includes initialization and tracking code example

### US-014: Update SDK READMEs to reference llms.txt

**Description:** As a developer or agent browsing an SDK repo, I want the README to mention the llms.txt file so I know there's an agent-optimized guide available.

**Acceptance Criteria:**

- [ ] Each SDK repo's README.md gets a small section (1-2 lines) at the top or bottom referencing llms.txt
- [ ] Text like: "For AI/LLM integration instructions, see [llms.txt](./llms.txt)"
- [ ] Applied to all 13 repos (aptabase-electron, tauri-plugin-aptabase, aptabase-swift, aptabase-kotlin, aptabase_flutter, aptabase-react-native, aptabase-maui, aptabase-js, aptabase-python, aptabase-unity, aptabase-unreal, aptabase-cpp)
- [ ] NativeScript excluded (community-maintained, different org)

## Functional Requirements

- FR-1: Every `llms.txt` must follow the [llms.txt spec](https://llmstxt.org/) format
- FR-2: Every `llms.txt` must include: product name, SDK package name, install command, initialization code, `trackEvent()` usage with and without properties
- FR-3: All code examples must be syntactically correct and match the current SDK version
- FR-4: The file must be named exactly `llms.txt` (lowercase) at the repo root
- FR-5: Content must be sourced from existing SDK READMEs and documentation — no invented APIs
- FR-6: Each file should be self-contained (an agent should not need to read any other file to integrate the SDK)

## Non-Goals

- No changes to SDK source code or APIs
- No auto-generation of llms.txt from code (manual for now)
- No AGENTS.md files (these repos are libraries, not projects agents work within)
- No NativeScript llms.txt (community-maintained by nstudio)
- No changes to package registry descriptions (npm, pub.dev, etc.)

## Technical Considerations

- **13 separate repos** need PRs — consider batching or scripting the creation
- Each repo may have a different default branch name (main vs master)
- The aptabase-js monorepo contains 4 packages — its llms.txt must cover all 4
- SDK versions change — llms.txt should use generic version references or "latest" where possible
- Some SDKs have minimal READMEs (e.g., aptabase-kotlin) — llms.txt may need to be written from scratch based on source code

## Success Metrics

- All 13 Aptabase-owned SDK repos have a `llms.txt` at root
- An AI agent given a repo URL can generate a working integration from llms.txt alone
- Consistent structure across all SDK llms.txt files (same sections, same format)
- No incorrect install commands or API signatures in any llms.txt

## Open Questions

- Should we create a script that validates llms.txt files across all SDK repos?
  Answer: No
- Should the llms.txt files link back to `aptabase.com/llms.txt` for cross-discovery?
  Answer: Yes
- How do we keep llms.txt in sync when SDK APIs change? (CI check? Manual process?)
  Answer: Manual
