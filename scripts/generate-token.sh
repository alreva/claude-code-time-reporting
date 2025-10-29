#!/usr/bin/env bash
# Generate a secure random bearer token for API authentication

set -euo pipefail

# Generate 32 bytes of random data, encode as base64
TOKEN=$(openssl rand -base64 32)

echo "Generated Bearer Token:"
echo "======================"
echo "$TOKEN"
echo ""
echo "Note: Use ./setup.sh instead to automatically configure everything"
echo ""
echo "Or manually add to your env.sh:"
echo "export Authentication__BearerToken=$TOKEN"
echo ""
echo "Use this in API requests:"
echo "Authorization: Bearer $TOKEN"
