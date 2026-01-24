#!/bin/bash

# Test script for GET /api/v0/apps/{appId}/errors endpoint
# This endpoint requires authentication, so it cannot be tested with curl alone
# You need to:
# 1. Start the dev server: cd src && dotnet watch
# 2. Login to the application via browser to get a session cookie
# 3. Use browser dev tools or Postman with the session cookie to test

BASE_URL="https://localhost:5251"
APP_ID="your-app-id-here"  # Replace with an actual app ID from your database

echo "Testing GET /api/v0/apps/{appId}/errors endpoint..."
echo ""
echo "NOTE: This endpoint requires authentication."
echo "To test, you need to:"
echo "1. Start dev server: cd src && dotnet watch"
echo "2. Login via browser and get a session cookie"
echo "3. Use the following curl command with your session cookie:"
echo ""
echo "curl -X GET \"$BASE_URL/api/v0/apps/$APP_ID/errors?startDate=2026-01-17T00:00:00Z&endDate=2026-01-24T23:59:59Z&offset=0&limit=10\" \\"
echo "  -H 'Cookie: .aptabase.auth=YOUR_SESSION_COOKIE_HERE' \\"
echo "  -k"
echo ""
echo "Expected response:"
echo "{"
echo "  \"errors\": ["
echo "    {"
echo "      \"errorId\": \"uuid\","
echo "      \"appId\": \"app-id\","
echo "      \"timestamp\": \"2026-01-24T12:00:00Z\","
echo "      \"errorMessage\": \"Example error message\","
echo "      \"errorType\": \"TypeError\","
echo "      \"stackTrace\": \"...\","
echo "      \"platform\": \"iOS\","
echo "      \"osName\": \"iOS\","
echo "      \"osVersion\": \"17.0\","
echo "      \"appVersion\": \"1.0.0\","
echo "      \"sdkVersion\": \"1.0.0\","
echo "      \"sessionId\": \"session-id\""
echo "    }"
echo "  ],"
echo "  \"pagination\": {"
echo "    \"offset\": 0,"
echo "    \"limit\": 10,"
echo "    \"total\": 1"
echo "  }"
echo "}"
