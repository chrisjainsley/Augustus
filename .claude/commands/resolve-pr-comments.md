# Resolve PR Review Comments

Fetch all unresolved review comments on a pull request and fix them.

## Arguments

$ARGUMENTS — the PR number (e.g. `28`) or a GitHub PR URL. If empty, detect the PR for the current branch using `gh pr view --json number`.

## Steps

1. **Identify the PR**
   - If `$ARGUMENTS` is a URL, extract the PR number from it.
   - If `$ARGUMENTS` is a number, use it directly.
   - If `$ARGUMENTS` is empty, run `gh pr view --json number --jq .number` to detect the PR for the current branch.

2. **Fetch review comments**
   - Run: `gh api repos/{owner}/{repo}/pulls/{pr}/comments --jq '.[] | {id: .id, path: .path, line: .line, body: .body, in_reply_to_id: .in_reply_to_id}'`
   - Also fetch review-level comments: `gh pr view {pr} --json reviews --jq '.reviews[] | {author: .author.login, state: .state, body: .body}'`
   - Filter to unresolved/actionable comments (skip pure praise, skip replies that are just acknowledgements).

3. **Fetch CI status**
   - Run: `gh pr view {pr} --json statusCheckRollup --jq '.statusCheckRollup[] | {name: .name, status: .status, conclusion: .conclusion}'`
   - If any check failed, fetch the logs: `gh run view {run_id} --log-failed`

4. **For each actionable comment**
   - Read the file at the referenced path and line.
   - Understand what the reviewer is asking for.
   - Implement the fix with minimal, targeted changes.
   - If a comment suggests something that would break existing behavior or is incorrect, note it but do not blindly apply it — use your judgment.

5. **For each CI failure**
   - Read the failed log output.
   - Diagnose the root cause.
   - Fix the issue.

6. **Build and test**
   - Run `dotnet build` to verify compilation.
   - Run `dotnet test` to verify all tests pass.
   - If tests fail, diagnose and fix before proceeding.

7. **Commit and push**
   - Stage all changed files.
   - Write a clear commit message summarizing what was fixed and why.
   - Push to the PR branch.

8. **Reply to and resolve AI-generated comments**
   After pushing, for each comment that was addressed:
   - Identify the review thread ID using the GraphQL API:
     ```
     gh api graphql -f query='query { repository(owner: "{owner}", name: "{repo}") { pullRequest(number: {pr}) { reviewThreads(first: 50) { nodes { id isResolved comments(first: 1) { nodes { databaseId } } } } } } }'
     ```
   - Match each thread to the comment by `databaseId` (the REST API comment `id`).
   - **Reply** to the comment thread explaining what was done:
     ```
     gh api repos/{owner}/{repo}/pulls/{pr}/comments/{comment_id}/replies -f body="Fixed: <brief description of the fix>"
     ```
   - **Resolve** the thread (only for AI-generated comments, i.e. `user.login` is `Copilot`, `github-actions[bot]`, or similar bot accounts — never auto-resolve human reviewer comments):
     ```
     gh api graphql -f query='mutation { resolveReviewThread(input: {threadId: "{thread_id}"}) { thread { isResolved } } }'
     ```
   - Skip resolving threads for comments that were intentionally not addressed.

9. **Report**
   - List each comment that was addressed and what was changed.
   - List any comments that were intentionally skipped and why.
   - Show CI status.
   - Show which comment threads were replied to and resolved.

## Important Notes

- Read the actual file before making changes — don't guess at surrounding code.
- Make targeted fixes; don't refactor unrelated code.
- If a reviewer suggestion conflicts with project conventions or would introduce a bug, skip it and explain why in the report.
- Preserve existing test behavior.
- Do NOT force-push or amend commits.
- Only auto-resolve comments from bot/AI reviewers. Never auto-resolve comments from human reviewers — those require explicit human sign-off.
