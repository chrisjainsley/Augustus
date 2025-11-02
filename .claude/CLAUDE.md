# Augustus Project - Claude Code Guidelines

This file documents best practices and automation workflows for the Augustus project development.

## Branch and Worktree Management

**When to use the branch-worktree-manager agent:**

- Starting a new feature branch
- Creating a bugfix or hotfix branch
- Setting up parallel worktrees for simultaneous work
- Cleaning up or reorganizing existing branches
- Validating that branch names follow semantic conventions

**How to trigger:**

When you request branch or worktree work, the branch-worktree-manager agent will:
1. Clarify the purpose and context of your work
2. Generate semantic-compliant branch names (e.g., `feature/oauth-support`, `bugfix/fix-cache-race-condition`)
3. Create branches with proper base branches (feature from develop/main, hotfix from main)
4. Optionally create linked worktrees for parallel development
5. Provide complete Git commands with explanations

**Example requests:**
- "I need to create a branch for adding GPT-5 support"
- "Set up a worktree so I can work on two features simultaneously"
- "Clean up old branches that aren't being used"

## Automated Code Review

**Automatic execution via Stop hook:**

A Claude Code Stop hook is configured to automatically run `/review-and-fix` when Claude stops processing. This ensures consistent code quality after each development session.

**The code review process:**

1. Review all C# source files for quality issues
2. Identify and prioritize issues (Critical, High, Medium, Low)
3. Fix Critical and High priority issues automatically
4. Run tests to verify all fixes work
5. Report findings and recommendations

**Manual execution:**

You can also manually trigger the review at any time by using the `/review-and-fix` command.

See `.claude/commands/review-and-fix.md` for detailed review criteria.

---

**Note**: These guidelines help maintain code quality, consistency, and proper Git workflow throughout development.
