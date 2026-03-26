# Augustus Project - Claude Code Workflows

Automation workflows for Claude Code in this project. General project guidance is in the root `CLAUDE.md`.

## Branch and Worktree Management

When you request branch or worktree work, the branch-worktree-manager agent will generate semantic branch names, create branches with proper base branches, and optionally set up linked worktrees for parallel development.

## Automated Code Review

A Stop hook runs an agent-based code review when Claude stops processing. It reviews C# source files, fixes Critical/High issues, and runs tests. Trigger manually with `/review-and-fix`.

See `.claude/commands/review-and-fix.md` for review criteria.
