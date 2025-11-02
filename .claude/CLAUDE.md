# Automated Code Review Instructions

**IMPORTANT**: After completing any code changes to the Augustus project, automatically:

1. Check if substantial changes were made to C# source files
2. If yes, run a code review by executing the review-and-fix workflow
3. Report findings and implement critical/high-priority fixes
4. Run tests to verify fixes
5. Report completion status

## When to Auto-Review

- After editing 3+ files
- After adding new features
- After refactoring
- Before the user creates a commit/PR
- When the user requests it

## How to Execute

Follow the instructions in `.claude/commands/review-and-fix.md` to:
1. Review all code
2. Prioritize issues
3. Implement fixes
4. Test changes
5. Report results

---

**Note**: This file serves as a reminder to maintain code quality throughout the development process.
