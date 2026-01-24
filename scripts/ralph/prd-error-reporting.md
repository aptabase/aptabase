# PRD: Error Reporting

## Introduction

Add error reporting capabilities to Aptabase, allowing developers to capture and analyze crashes and errors from their applications across all platforms (web, mobile, desktop). This feature will provide standard error details including error messages, stack traces, and device/browser information, with automatic PII sanitization to maintain privacy standards.

## Goals

- Enable error/crash capture from all Aptabase SDKs (Web, Mobile, Desktop)
- Store error message, stack trace, and contextual device/browser information
- Provide basic error viewing and filtering in the dashboard
- Automatically sanitize PII from error data to maintain privacy-first approach
- Implement as a separate feature with dedicated storage and UI
- Keep it simple and focused (MVP scope)

## User Stories

### US-001: Database schema for error storage
**Description:** As a developer, I need a database schema to store error events separately from analytics events.

**Acceptance Criteria:**
- [ ] Create new ClickHouse/Tinybird table for error events with fields: error_id, app_id, timestamp, error_message, error_type, stack_trace, platform, os_name, os_version, app_version, sdk_version, session_id
- [ ] Set TTL to 180 days for error data retention
- [ ] Add migration for PostgreSQL to track error quotas/limits per app
- [ ] Migration runs successfully on both ClickHouse and Tinybird backends
- [ ] Typecheck passes

### US-002: Error ingestion API endpoint
**Description:** As an SDK developer, I need an API endpoint to send error/crash data from client applications.

**Acceptance Criteria:**
- [ ] Create POST `/api/v0/error` endpoint that accepts error payloads
- [ ] Validate required fields (app_id, timestamp, error_message, error_type)
- [ ] Store errors in in-memory buffer similar to event ingestion pattern
- [ ] Return 202 Accepted on successful ingestion
- [ ] Apply rate limiting (use existing EventIngestion policy: 20 req/sec per IP)
- [ ] Typecheck passes
- [ ] Test with curl/Postman to verify ingestion works

### US-003: PII sanitization for errors
**Description:** As a privacy-conscious platform, I need to automatically sanitize PII from error messages and stack traces.

**Acceptance Criteria:**
- [ ] Implement sanitization function that redacts: emails, IP addresses, API keys/tokens, credit card numbers, phone numbers
- [ ] Apply sanitization to error_message and stack_trace before storage
- [ ] Use regex patterns for common PII formats
- [ ] Preserve stack trace structure while redacting values
- [ ] Add unit tests for sanitization function
- [ ] All tests pass

### US-004: Background worker for error persistence
**Description:** As a system, I need to flush buffered errors to the database periodically to ensure data persistence.

**Acceptance Criteria:**
- [ ] Create ErrorBackgroundWriter similar to EventBackgroundWriter
- [ ] Flush errors every 5 seconds or when buffer reaches 1000 errors
- [ ] Write to ClickHouse or Tinybird based on configuration
- [ ] Log errors if write fails but don't crash the service
- [ ] Typecheck passes

### US-005: Query service for retrieving errors
**Description:** As a backend developer, I need a query service to retrieve errors from the database for API endpoints.

**Acceptance Criteria:**
- [ ] Create IErrorQueryClient interface with methods: GetErrors, GetErrorById, GetErrorCount
- [ ] Implement for both ClickHouse and Tinybird
- [ ] Support filtering by: app_id, date range, error_type, platform
- [ ] Support pagination (offset/limit)
- [ ] Return errors ordered by timestamp DESC
- [ ] Typecheck passes

### US-006: Error list API endpoint
**Description:** As a frontend developer, I need an API endpoint to fetch error lists for display in the dashboard.

**Acceptance Criteria:**
- [ ] Create GET `/api/v0/apps/{appId}/errors` endpoint
- [ ] Accept query params: startDate, endDate, errorType, platform, offset, limit
- [ ] Require authentication (user must own the app)
- [ ] Return paginated list of errors with metadata
- [ ] Apply rate limiting (same as Stats: 1000 req/hour per IP)
- [ ] Typecheck passes
- [ ] Test with curl to verify response format

### US-007: Error detail API endpoint
**Description:** As a frontend developer, I need an API endpoint to fetch individual error details including full stack trace.

**Acceptance Criteria:**
- [ ] Create GET `/api/v0/apps/{appId}/errors/{errorId}` endpoint
- [ ] Require authentication (user must own the app)
- [ ] Return full error details including sanitized stack trace
- [ ] Return 404 if error not found or doesn't belong to app
- [ ] Typecheck passes

### US-008: Error dashboard page (UI)
**Description:** As a user, I want to view a list of errors for my app in a dedicated dashboard page.

**Acceptance Criteria:**
- [ ] Create new route `/apps/{appId}/errors` in React Router
- [ ] Add "Errors" navigation item in app sidebar
- [ ] Display error list in table format with columns: timestamp, error_type, error_message (truncated), platform
- [ ] Show loading state while fetching
- [ ] Show empty state if no errors
- [ ] Implement pagination controls
- [ ] Typecheck passes
- [ ] Verify in browser using dev-browser skill

### US-009: Error filtering UI
**Description:** As a user, I want to filter errors by date range, error type, and platform to find specific issues.

**Acceptance Criteria:**
- [ ] Add date range picker (default: last 7 days)
- [ ] Add platform filter dropdown (All, iOS, Android, Windows, macOS, Linux, Web)
- [ ] Add error type filter dropdown (All, plus dynamic list from errors)
- [ ] Filters update URL params for shareability
- [ ] Apply filters button triggers new API request
- [ ] Typecheck passes
- [ ] Verify in browser using dev-browser skill

### US-010: Error detail modal
**Description:** As a user, I want to click on an error in the list to see full details including stack trace.

**Acceptance Criteria:**
- [ ] Clicking error row opens modal with full error details
- [ ] Display: timestamp, error_type, error_message, platform, OS info, app_version, SDK version
- [ ] Display stack trace in monospace font with copy button
- [ ] Show device/browser info section
- [ ] Modal has close button and ESC key closes it
- [ ] Typecheck passes
- [ ] Verify in browser using dev-browser skill

## Functional Requirements

**Error Ingestion:**
- FR-1: System must accept error events via POST `/api/v0/error` with app key authentication
- FR-2: Error payload must include: app_id, timestamp, error_message, error_type, platform
- FR-3: Error payload may include: stack_trace, os_name, os_version, app_version, sdk_version, session_id
- FR-4: System must buffer errors in memory and flush periodically (5 sec or 1000 errors)
- FR-5: System must sanitize PII (emails, IPs, tokens, credit cards, phone numbers) before storage

**Error Storage:**
- FR-6: Errors must be stored in separate ClickHouse/Tinybird table from analytics events
- FR-7: Each error must have unique error_id for retrieval
- FR-8: Errors must be associated with app_id for access control

**Error Retrieval:**
- FR-9: Users must authenticate to view errors for their apps only
- FR-10: Error list API must support filtering by date range, error_type, platform
- FR-11: Error list must be paginated with configurable limit
- FR-12: Errors must be sorted by timestamp descending (newest first)

**Error Display:**
- FR-13: Dashboard must show error list with key details visible at a glance
- FR-14: Users can click error to view full details in modal
- FR-15: Stack traces must be displayed in monospace font with copy functionality
- FR-16: Filters must persist in URL for shareability

## Non-Goals

- No error grouping/deduplication (future enhancement)
- No error alerts or notifications
- No error resolution/status tracking
- No source map support for JavaScript stack traces
- No breadcrumbs or user action history
- No custom error metadata/tags
- No error charts or trend visualization (this MVP is just basic listing)
- No error search functionality
- No bulk operations on errors

## Design Considerations

**UI/UX:**
- Reuse existing table components from analytics dashboard
- Use consistent color scheme and typography
- Error detail modal should be similar to existing modals in the app
- Stack trace should be easily readable and copyable

**Error States:**
- Loading: Show skeleton loaders for table
- Empty: "No errors found" with helpful message
- Error: Show error banner if API fails

## Technical Considerations

**Backend:**
- Follow existing patterns in `Features/Ingestion/` for event buffering
- Create new `Features/Errors/` directory for error-specific code
- Reuse authentication middleware from existing API endpoints
- Store errors in ClickHouse/Tinybird (same backend as analytics)

**Frontend:**
- Create new feature directory: `src/webapp/features/errors/`
- Use existing hooks for data fetching (useFetch or similar)
- Use Jotai atoms if state sharing needed across components
- Follow existing table/modal patterns from other features

**Privacy:**
- PII sanitization must be thorough but not overzealous
- Preserve enough context in errors for debugging
- Document what gets sanitized in user-facing docs

**Performance:**
- Error ingestion must not impact analytics event ingestion
- Use separate buffer and background writer
- Consider error quotas to prevent abuse (future consideration)

**Database Schema Notes:**
- ClickHouse table should be optimized for time-series queries
- TTL/retention policy: 180 days for error data
- Index on (app_id, timestamp) for fast filtering
- session_id field links errors to analytics sessions for correlation

## Success Metrics

- Errors are successfully ingested from test SDK calls
- PII sanitization removes all test patterns (100% match rate)
- Error dashboard loads in under 2 seconds
- Users can find and view error details in under 3 clicks
- No regression in analytics event ingestion performance

## Open Questions

- Should we limit error payload size (e.g., max 100KB per error)?

## Decisions Made

- **Retention period:** 180 days for error data
- **Session correlation:** Yes, session_id links errors to analytics sessions for full context
- **Rate limiting:** Apply existing EventIngestion rate limit policy (20 req/sec per IP)
