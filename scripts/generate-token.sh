#!/usr/bin/env bash
# Generate a secure random bearer token for API authentication

set -euo pipefail

# Generate 32 bytes of random data, encode as base64
TOKEN=$(openssl rand -base64 32)

echo "Generated Bearer Token:"
echo "======================"
echo "$TOKEN"
echo ""
echo "Add this to your .env file:"
echo "BEARER_TOKEN=$TOKEN"
echo ""
echo "Use this in API requests:"
echo "Authorization: Bearer $TOKEN"
