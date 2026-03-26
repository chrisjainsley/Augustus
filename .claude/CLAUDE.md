# Augustus Project - Claude Code Workflows

Automation workflows for Claude Code in this project. General project guidance is in the root `CLAUDE.md`.

## Branch and Worktree Management

When you request branch or worktree work, the branch-worktree-manager agent will generate semantic branch names, create branches with proper base branches, and optionally set up linked worktrees for parallel development.

## Automated Code Review

You can optionally configure a Stop hook in `.claude/settings.json` to run agent-based review when Claude stops processing. That review can scan C# source files, fix Critical/High issues, and run tests. Trigger manually with `/review-and-fix`.

See `.claude/commands/review-and-fix.md` for review criteria.
