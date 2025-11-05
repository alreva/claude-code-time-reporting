#!/usr/bin/env bash
# DEPRECATED: This script generated bearer tokens for the old authentication system
#
# The system now uses Azure Entra ID authentication.
# Please use the following instead:

set -euo pipefail

echo "⚠️  DEPRECATED: Bearer token authentication has been replaced"
echo ""
echo "The system now uses Azure Entra ID authentication."
echo ""
echo "To set up authentication:"
echo "  1. az login"
echo "  2. ./setup.sh"
echo "  3. source env.sh"
echo ""
echo "To get a token for testing:"
echo "  az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 --query accessToken -o tsv"
echo ""
exit 1
