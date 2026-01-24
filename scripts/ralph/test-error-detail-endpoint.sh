#!/bin/bash

# Test script for GET /api/v0/apps/{appId}/errors/{errorId} endpoint
# This endpoint requires authentication, so you must be logged in with a valid session cookie

# Example usage (requires authentication):
# You'll need:
# 1. A valid session cookie from logging into the app
# 2. An app_id that you own
# 3. An error_id that exists for that app

echo "To test the error detail endpoint, use:"
echo ""
echo "curl -X GET 'https://localhost:5251/api/v0/apps/{appId}/errors/{errorId}' \\"
echo "  -H 'Cookie: .aptabase.auth=YOUR_SESSION_COOKIE' \\"
echo "  --insecure"
echo ""
echo "Expected response (200 OK):"
echo "{"
echo "  \"errorId\": \"00000000-0000-0000-0000-000000000000\","
echo "  \"appId\": \"app123\","
echo "  \"timestamp\": \"2026-01-24T10:30:00Z\","
echo "  \"errorMessage\": \"Uncaught TypeError: Cannot read property 'foo' of null\","
echo "  \"errorType\": \"TypeError\","
echo "  \"stackTrace\": \"at MyComponent.render (App.tsx:42)\\nat renderComponent (react-dom.js:123)\","
echo "  \"platform\": \"Web\","
echo "  \"osName\": \"macOS\","
echo "  \"osVersion\": \"14.0\","
echo "  \"appVersion\": \"1.2.3\","
echo "  \"sdkVersion\": \"0.1.0\","
echo "  \"sessionId\": \"session-123\""
echo "}"
echo ""
echo "Expected response (404 Not Found) if error doesn't exist or doesn't belong to app:"
echo "{"
echo "  \"error\": \"Error not found\""
echo "}"
echo ""
echo "Expected response (403 Forbidden) if user doesn't have access to app:"
echo "(No body, just 403 status code)"
