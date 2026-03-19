Merge the latest master branch into the current branch, resolving any conflicts intelligently.

## Step 1: Pre-flight checks

Run these commands to confirm the workspace is ready:

```bash
git status
git branch --show-current
```

- If there are uncommitted changes, stop and tell the user to commit or stash first.
- Note the current branch name — you'll need it throughout.

## Step 2: Fetch and merge master

```bash
git fetch origin master
git merge origin/master
```

If the merge completes cleanly (no conflicts), skip to Step 6.

## Step 3: Assess conflicts

If the merge produced conflicts, run:

```bash
git diff --name-only --diff-filter=U
```

This lists all conflicted files. For each file, read the full file to see the conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`).

Categorize each conflict:

- **Trivial**: whitespace, import ordering, non-overlapping additions in the same file. Resolve these immediately by keeping both changes or picking the obvious winner.
- **Complex**: overlapping logic changes, structural refactors, renamed/moved code, or conflicts where the intent of both sides needs to be understood to merge correctly.

Resolve all trivial conflicts now. For complex conflicts, proceed to Step 4.

## Step 4: Plan complex conflict resolution

Enter plan mode to analyze complex conflicts before touching them.

For each complex conflict, the plan should cover:

1. **What master changed** — read the master version and understand the intent
2. **What this branch changed** — read the branch version and understand the intent
3. **Are the changes compatible?** — Can both changes coexist, or does one supersede the other?
4. **Resolution strategy** — Exactly how to merge: keep both, prefer one side, or write a combined version
5. **Risk assessment** — What could break if the resolution is wrong? Which tests cover this area?

Present the plan to the user for approval before proceeding.

## Step 5: Execute conflict resolution

After the plan is approved, resolve each complex conflict:

- Remove all conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`)
- Apply the resolution strategy from the plan
- Read the resolved file in full to verify it makes sense as a whole — not just the conflict region
- Stage each resolved file:

```bash
git add <resolved-file>
```

After all conflicts are resolved, verify no conflict markers remain:

```bash
git grep -r "<<<<<<< " -- ":(exclude).git" || echo "No conflict markers found"
```

## Step 6: Verify the merge

Run a build to make sure nothing is broken:

```bash
dotnet build Augustus/Augustus.sln --configuration Release
```

If the build fails, diagnose and fix. Build failures after a merge are often caused by:
- Missing `using` statements from one side of the conflict
- Duplicate method/class definitions
- API signature changes on master that the branch code hasn't adapted to

Run tests:

```bash
dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj --configuration Release --no-build --verbosity normal
```

If tests fail, fix them before completing.

## Step 7: Complete the merge

Once the build passes and tests are green, complete the merge.

- If the merge had conflicts (merge is still pending), run `git commit --no-edit` to finalize with the default merge message.
- If the merge already completed (clean merge, no conflicts), there is nothing to commit — the merge commit was created automatically.

Report what was done:
- How many files had conflicts
- How many were trivial vs complex
- What resolution strategies were used for complex conflicts
- Build and test status

## Rules

- Never force-push or rewrite history during a merge.
- Never silently drop changes from either side — if in doubt, keep both and flag it.
- If a conflict involves deleted vs modified (one side deleted a file, the other modified it), ask the user which side wins.
- If the merge introduces more than 5 complex conflicts, pause after the plan step and confirm the user wants to proceed rather than rebasing or cherry-picking instead.
