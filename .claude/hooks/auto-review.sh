#!/bin/bash
# Auto Code Review Hook for Claude Code
# This runs automatically after Claude finishes editing files

echo "🔍 Starting automated code review..."

# Check if there were any file changes
if git diff --cached --quiet && git diff --quiet; then
    echo "ℹ️  No changes detected, skipping review"
    exit 0
fi

echo "📝 Changes detected, running comprehensive review..."

# This will be picked up by Claude Code to run the review
echo "CLAUDE_COMMAND: /review-and-fix"

exit 0
