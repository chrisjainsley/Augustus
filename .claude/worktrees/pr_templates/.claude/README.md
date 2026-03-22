# Augustus Claude Code Configuration

This directory contains Claude Code hooks and commands for the Augustus project.

## Available Commands

### `/review-and-fix`

Runs a comprehensive automated code review and fix cycle:

1. **Code Review**: Analyzes all source code for issues (bugs, security, performance, quality)
2. **Auto-Fix**: Implements fixes for critical and high-priority issues
3. **Testing**: Builds the project and runs all tests
4. **Report**: Provides a summary of changes and recommendations

**Usage:**
```
/review-and-fix
```

This is perfect for:
- Before creating a pull request
- After making significant changes
- Regular code quality checks
- Catching issues early

## Automatic Code Review Hook

### Setting Up Auto-Review After Edits

To have Claude automatically review code after completing edits, configure a hook in your Claude Code settings:

1. Open Claude Code Settings (or your `.claude/settings.json`)
2. Add a user prompt hook:

```json
{
  "hooks": {
    "user-prompt-submit": {
      "command": "bash .claude/hooks/auto-review.sh",
      "description": "Run code review after Claude finishes"
    }
  }
}
```

**Or use the simpler approach:**

Add this to your settings to run review after every response:
```json
{
  "auto_review_on_complete": true
}
```

### Manual Trigger

You can also manually trigger the review anytime by typing:
```
/review-and-fix
```

## How It Works

1. The command file (`.claude/commands/review-and-fix.md`) contains instructions for Claude
2. When you run `/review-and-fix`, Claude follows those instructions
3. Claude will:
   - Review all C# files
   - Identify and prioritize issues
   - Implement fixes
   - Run tests
   - Report results

## Benefits

- **Automated Quality Assurance**: Catch issues before they become problems
- **Consistent Standards**: Apply .NET best practices automatically
- **Time Saving**: No manual code review needed for common issues
- **Learning Tool**: See what issues Claude identifies and how to fix them
- **CI/CD Ready**: Ensure code quality before pushing

## Customization

You can modify `.claude/commands/review-and-fix.md` to:
- Add project-specific rules
- Change priority levels
- Focus on specific areas
- Add custom checks

## Example Output

When you run `/review-and-fix`, expect output like:

```
🔍 Reviewing code...

Found 3 issues:
1. [HIGH] Missing null check in ResponseGenerator.cs:45
2. [MEDIUM] Inefficient string concatenation in FileManager.cs:78
3. [LOW] Variable could be const in APISimulator.cs:23

🔧 Implementing fixes...

✅ Fixed: Added null check with ArgumentNullException
✅ Fixed: Replaced string concat with StringBuilder
⏭️  Skipped: Low priority style issue

🧪 Running tests...

Build: ✅ SUCCESS
Tests: ✅ 9 passed, 0 failed

📊 Summary:
- Fixed 2 critical/high priority issues
- All tests passing
- 1 minor suggestion for future improvement
```

## Tips

- Run `/review-and-fix` regularly during development
- Review the changes Claude makes to learn best practices
- Commit the fixes Claude makes with clear commit messages
- Use this before creating pull requests

## Support

For issues or suggestions about these commands, see the main Augustus documentation.
