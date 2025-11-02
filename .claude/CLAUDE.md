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

**When to run auto-review:**

After making substantial changes to C# source files:
- After editing 3+ files
- After adding new features
- After refactoring
- Before creating commits or PRs
- When the user explicitly requests it

**How to trigger:**

The code review process will:
1. Review all C# source files for quality issues
2. Identify and prioritize issues (Critical, High, Medium, Low)
3. Fix Critical and High priority issues automatically
4. Run tests to verify all fixes work
5. Report findings and recommendations

See `.claude/commands/review-and-fix.md` for detailed review criteria.

## Useful Custom Hooks

The project includes automated scripts that can be manually executed:
- `.claude/hooks/auto-review.sh` - Run comprehensive code review (see review process above)

---

**Note**: These guidelines help maintain code quality, consistency, and proper Git workflow throughout development.
