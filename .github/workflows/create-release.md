---
name: Create Release
description: >
  Runs releasability checks (build, test, code review), analyzes changes since
  the last release, selects the best semantic version increment, generates
  release notes, creates a draft GitHub release (cleaning up stale drafts),
  and opens a review issue for human approval before the release is published.

on:
  workflow_dispatch:
    inputs:
      dry_run:
        description: "Run in test mode — analyse and report without creating the release or review issue"
        required: false
        default: 'false'
        type: boolean

permissions:
  contents: read
  issues: read
  pull-requests: read

network:
  allowed:
    - defaults
    - dotnet

tools:
  github:
    mode: remote
    toolsets: [default]
  bash: true

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

timeout-minutes: 30

safe-outputs:
  create-issue:
    max: 1
    title-prefix: "[Release Review] "
    labels: [release]
  jobs:
    create_draft_release:
      description: >
        Deletes any existing stale draft releases, then creates a new draft
        GitHub release with the specified version tag, title, and release
        notes. The release is created as a draft so a human can review and
        publish it. Returns the URL of the releases page.
      runs-on: ubuntu-latest
      permissions:
        contents: write
      inputs:
        tag_name:
          description: "Version tag for the release (e.g. v1.2.3)"
          required: true
          type: string
        release_name:
          description: "Human-readable release title (e.g. Augustus 1.2.3)"
          required: true
          type: string
        release_notes:
          description: "Markdown-formatted release notes"
          required: true
          type: string
        prerelease:
          description: "Set to 'true' if this is a pre-release (alpha/beta/rc)"
          required: false
          type: string
      steps:
        - name: Clean up stale drafts and create draft release
          env:
            GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
            DRY_RUN: ${{ inputs.dry_run }}
          run: |
            TAG=$(jq -r '.items[] | select(.type == "create_draft_release") | .tag_name' "$GH_AW_AGENT_OUTPUT")
            NAME=$(jq -r '.items[] | select(.type == "create_draft_release") | .release_name' "$GH_AW_AGENT_OUTPUT")
            PRERELEASE=$(jq -r '.items[] | select(.type == "create_draft_release") | .prerelease // "false"' "$GH_AW_AGENT_OUTPUT")
            jq -r '.items[] | select(.type == "create_draft_release") | .release_notes' "$GH_AW_AGENT_OUTPUT" > /tmp/release-notes.md

            # --- Delete existing draft release for this tag (if any) ---
            echo "Checking for existing draft release for tag: $TAG"
            EXISTING_DRAFT=$(gh release list --repo "$GITHUB_REPOSITORY" --json tagName,isDraft --jq '.[] | select(.isDraft) | .tagName' | awk -v tag="$TAG" '$0 == tag { print; exit }')
            if [ -n "$EXISTING_DRAFT" ]; then
              if [ "$DRY_RUN" = "true" ]; then
                echo "[DRY RUN] Would delete existing draft release for tag: $EXISTING_DRAFT"
              else
                echo "Deleting existing draft release for tag: $EXISTING_DRAFT"
                gh release delete "$EXISTING_DRAFT" --repo "$GITHUB_REPOSITORY" --yes --cleanup-tag 2>/dev/null || true
              fi
            else
              echo "No existing draft release found for tag: $TAG"
            fi

            # --- Create the new draft release ---
            FLAGS="--draft"
            if [ "$PRERELEASE" = "true" ]; then
              FLAGS="$FLAGS --prerelease"
            fi

            if [ "$DRY_RUN" = "true" ]; then
              echo "[DRY RUN] Would create draft release:"
              echo "  Tag:         $TAG"
              echo "  Name:        $NAME"
              echo "  Pre-release: $PRERELEASE"
              echo "  Flags:       $FLAGS"
              echo "  Notes:"
              cat /tmp/release-notes.md
              echo ""
              echo "Draft release NOT created (dry-run mode)."
            else
              gh release create "$TAG" \
                --repo "$GITHUB_REPOSITORY" \
                --title "$NAME" \
                --notes-file /tmp/release-notes.md \
                $FLAGS

              echo "Draft release created. Review at: https://github.com/$GITHUB_REPOSITORY/releases"
            fi
---
# Create Release

You are a release management assistant for the **Augustus** .NET library
(`Augustus.AI` and `Augustus.AI.Reqnroll` on NuGet). Both packages are published
from the same release with the same version number. Your task is to run
releasability checks, analyse changes since the last release, determine the
right semantic version increment, write release notes, create a draft GitHub
release for human review, and open a review issue.

> **Dry-run mode**: If this workflow was triggered with `dry_run = true`, complete
> Steps 0–4 as normal (checks, analysis, and release notes), then **skip
> Steps 5 and 6** — use `noop` instead and summarise what would have been
> created in your response (including any releasability findings). Do not
> create a real release or issue in dry-run mode.

## Step 0 — Releasability Check

Before analysing changes, verify the project is in a releasable state. Run the
checks below in order. **Blocker** failures stop the release; **warning**
findings are noted in the review issue but do not block.

### Blocker checks (fail = stop)

If **any** blocker check fails, do **not** proceed to Steps 1–6. Instead,
create a single issue titled
`[Release Review] Release Blocked — <short reason>` listing every failure and
critical code-review finding, then stop.

#### 0-A. Build check

```bash
dotnet restore Augustus/Augustus.sln
dotnet build Augustus/Augustus.sln --configuration Release
```

Both commands must exit `0`. Any build error is a blocker.

#### 0-B. Test check

```bash
dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj --configuration Release --verbosity normal
dotnet test Augustus/Augustus.Reqnroll.Tests/Augustus.Reqnroll.Tests.csproj --configuration Release --verbosity normal
```

All tests must pass. Pay special attention to **public API approval tests** —
failures there indicate unapproved API surface changes that must be resolved
before releasing.

#### 0-C. Code review

Review all source files changed since the last release tag for **critical**
issues only. Use the GitHub tools to identify changed files (`git diff` between
the last release tag and `HEAD`), then review each changed `.cs` file for:

- Security vulnerabilities (injection, exposed secrets, unsafe deserialization)
- Null reference risks in critical paths
- Unhandled exceptions or empty catch blocks
- Breaking API changes without documentation
- Race conditions or threading issues
- Resource leaks (undisposed `IDisposable`)

**Only critical findings block the release.** Medium and low findings should be
collected and included as warnings in the review issue (Step 6).

### Warning checks (do not block)

#### 0-D. TODO / FIXME / HACK markers

Scan files changed since the last release for `TODO`, `FIXME`, and `HACK`
comments. List them in the review issue so the maintainer is aware.

#### 0-E. Vulnerable dependencies

Run the following (best-effort — failure here does not block):

```bash
dotnet list Augustus/Augustus.sln package --vulnerable 2>/dev/null || true
```

If vulnerable packages are found, include them as a warning in the review issue.

---

If all blocker checks pass, proceed to Step 1.

## Step 1 — Find the Last Release

Run the following command to retrieve all releases:

```
gh release list --limit 10 --json tagName,publishedAt,isLatest,isDraft --repo "$GITHUB_REPOSITORY"
```

From the results, select the most recent **non-draft** release — the one where
`isLatest` is `true`, or the most recently published if none is marked latest.
Note its `tagName` (e.g. `v0.2.0`) and `publishedAt` date.

> ⚠️ **Do NOT use `git tag`, `git describe`, or any other git-based command
> to find releases.** This workflow uses a shallow clone (`--depth=1
> --no-tags`), so git has no tag history and will return empty results.
> The `gh release list` output is the **sole source of truth** for existing
> releases.
>
> If `gh release list` returns no results, this is the first release — use
> `v0.0.0` as the baseline for version calculation.

## Step 2 — Analyse Changes Since the Last Release

Use the GitHub tools to list all commits between the last release tag and the
current default branch (`master`). For each commit:

- Read the commit message and body.
- If the commit references a pull request, look it up for additional context
  (PR title, labels, description).
- Categorise the change into **one** of:
  - **Breaking Change** — removed or changed public API, renamed public types
    or members, changed method signatures in a backwards-incompatible way.
  - **New Feature** — new public API surface, new functionality, new
    configuration options.
  - **Bug Fix** — corrected incorrect behaviour, resolved a reported issue.
  - **Other** — documentation, refactoring, dependency updates, tests, CI
    changes.

## Step 3 — Select the Semantic Version Increment

Apply these rules **in order** to the categorised changes:

1. **MAJOR** (`X.0.0`) — if ANY breaking changes exist.
2. **MINOR** (`0.X.0`) — if any new **public-facing** features exist and NO
   breaking changes. A change qualifies as MINOR only if it adds new public
   API surface that library consumers can use — e.g. new public classes,
   methods, or configuration options.
3. **PATCH** (`0.0.X`) — if ONLY bug fixes, documentation, or internal
   changes. The following are always PATCH, never MINOR:
   - Internal security hardening (sanitization, encoding, validation) that
     does not add new public API
   - Caching, performance, or resilience improvements invisible to consumers
   - CI/CD workflow changes, build scripts, lock-file regeneration
   - Test additions or modifications
   - Documentation updates
   - Dependency bumps (unless they add new public API surface)

Calculate the new version from the last release version found in Step 1.

> ⚠️ The new version **must always be strictly greater than the last release
> tag**. Never propose a version that already exists or is lower than the
> current latest release.

**Note**: While the project is pre-1.0 (i.e. major is `0`):
- A change that would normally be MAJOR should still increment the **minor**
  segment (e.g. `0.2.0` → `0.3.0`) and document the breaking changes clearly.
- A MINOR change increments the **minor** segment (e.g. `0.2.0` → `0.3.0`).
- A PATCH change increments only the **patch** segment (e.g. `0.2.0` →
  `0.2.1`), never the minor segment.

> **Common mistake**: Internal improvements such as security hardening,
> sanitization, caching, and CI changes are **not** new features even if they
> touch core library code. Unless the change exposes a new public type, method,
> or configuration option that a consumer can call, it is a PATCH.

**Example**: If Step 1 found `v0.2.0` as the latest release and new features
were added, the correct next version is `v0.3.0` — not `v0.1.0` or any tag
that already exists.

## Step 4 — Write Release Notes

Produce well-structured release notes using this template (omit empty
sections):

```markdown
## What's Changed

### ⚠️ Breaking Changes
- <description>

### ✨ New Features
- <description>

### 🐛 Bug Fixes
- <description>

### 🔧 Other Changes
- <description>

**Full Changelog**: https://github.com/chrisjainsley/Augustus/compare/<last_tag>...<new_tag>
```

## Step 5 — Create a Draft Release

> **Note**: The `create_draft_release` job automatically deletes any existing
> draft release for the same tag before creating the new one, so there is no
> need to clean it up manually.

Call the `create_draft_release` tool with the following values:

- `tag_name` — new version tag prefixed with `v` (e.g. `v0.3.0`)
- `release_name` — display title (e.g. `Augustus 0.3.0`)
- `release_notes` — the markdown from Step 4
- `prerelease` — `"true"` if the version contains a pre-release suffix
  (`-alpha`, `-beta`, `-rc.N`), otherwise `"false"`

## Step 6 — Open a Human Review Issue

Create an issue so the maintainer can review the proposed release before it
goes live. The issue body must include:

1. **Proposed version** and the reason for the increment type (MAJOR / MINOR /
   PATCH), with a brief explanation.
2. **Releasability results** — summarise the outcome of Step 0:
   - Confirm that build, tests, and code review passed.
   - If there are **warnings** from Step 0 (medium/low code review findings,
     TODO/FIXME/HACK markers, vulnerable dependencies), list them in a
     collapsible `<details>` section titled "Releasability Warnings".
3. **Change summary table**:

   | Category | Count |
   |---|---|
   | ⚠️ Breaking Changes | N |
   | ✨ New Features | N |
   | 🐛 Bug Fixes | N |
   | 🔧 Other | N |

4. **Full release notes** (copy from Step 4).
5. **Next steps** for the reviewer:
   - **Approve via label**: Add the `release-approved` label to this issue to
     automatically publish the draft release. This triggers the
     `approve-release.yml` workflow which publishes the draft and closes this
     issue.
   - **Or publish manually**: Go to
     **[Releases → Drafts](https://github.com/chrisjainsley/Augustus/releases)**
     and open the draft release named `<release_name>`. Edit the release notes
     if needed, then click **"Publish release"**.
   - Publishing (by either method) automatically triggers NuGet packaging and
     publishing to NuGet.org via the existing CI workflow.
   - To cancel, delete the draft release and close this issue.

Use the issue title: `[Release Review] Augustus <version> - Release Ready for Review`
(e.g. `[Release Review] Augustus 0.3.0 - Release Ready for Review`).
