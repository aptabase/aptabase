#!/bin/bash

# Test script for POST /api/v0/error endpoint
# Usage: ./test-error-endpoint.sh [APP_KEY]
#
# Prerequisites:
# 1. Start Docker services: docker compose up -d
# 2. Start backend: cd src && dotnet watch
# 3. Ensure you have a valid APP_KEY from the database

APP_KEY=${1:-"your-app-key-here"}
BASE_URL="https://localhost:5251"

echo "Testing POST /api/v0/error endpoint..."
echo "App-Key: $APP_KEY"
echo ""

# Test 1: Valid error payload
echo "Test 1: Valid error payload"
curl -X POST "$BASE_URL/api/v0/error" \
  -H "Content-Type: application/json" \
  -H "App-Key: $APP_KEY" \
  -d '{
    "appId": "test-app",
    "timestamp": "'$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")'",
    "errorMessage": "Test error message",
    "errorType": "TestError",
    "stackTrace": "at TestFunction() in test.cs:line 42",
    "platform": "Web",
    "osName": "macOS",
    "osVersion": "14.0",
    "appVersion": "1.0.0",
    "sdkVersion": "1.0.0",
    "sessionId": "test-session-123"
  }' \
  -k -v

echo ""
echo ""

# Test 2: Missing required field (should fail)
echo "Test 2: Missing required field (errorType)"
curl -X POST "$BASE_URL/api/v0/error" \
  -H "Content-Type: application/json" \
  -H "App-Key: $APP_KEY" \
  -d '{
    "appId": "test-app",
    "timestamp": "'$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")'",
    "errorMessage": "Test error message"
  }' \
  -k -v

echo ""
echo ""

# Test 3: Missing App-Key header (should fail)
echo "Test 3: Missing App-Key header"
curl -X POST "$BASE_URL/api/v0/error" \
  -H "Content-Type: application/json" \
  -d '{
    "appId": "test-app",
    "timestamp": "'$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")'",
    "errorMessage": "Test error message",
    "errorType": "TestError"
  }' \
  -k -v

echo ""
echo "Tests completed!"
