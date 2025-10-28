#!/bin/bash

CONTEXT_FILE="$HOME/.config/time-reporting/session-context.json"

if [[ ! -f "$CONTEXT_FILE" ]]; then
    echo "❌ Session context file not found"
    echo "   Expected: $CONTEXT_FILE"
    exit 1
fi

echo "✅ Session context file exists"
echo
echo "Current context:"
if command -v jq &> /dev/null; then
    cat "$CONTEXT_FILE" | jq .
else
    cat "$CONTEXT_FILE"
fi
