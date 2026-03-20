# Augustus Release Process

This document describes how to release Augustus to NuGet.org using the automated CI/CD pipeline.

## Prerequisites

### 1. NuGet.org API Key

Before releasing, you need a NuGet.org API key:

1. Go to https://www.nuget.org/account/apikeys
2. Create a new API key with scopes:
   - `Push new packages and package versions`
   - `Push packages`
3. Store the key securely

### 2. GitHub Secret Configuration

Add the NuGet API key to your GitHub repository:

1. Go to your repository Settings → Secrets and variables → Actions
2. Create a new repository secret named `NUGET_API_KEY`
3. Paste your NuGet.org API key as the value
4. Save

### 3. `release-approved` Label

The approval workflow requires a label. Create it once:

```bash
gh label create "release-approved" --color "0E8A16" --description "Approves a draft release for publishing"
```

## Automated Release Workflow

Releases are managed by the **Create Release** agent workflow
(`.github/workflows/create-release.md`). It automates version selection,
release notes, releasability checks, and draft creation.

### Triggering a Release

1. Go to **Actions → Create Release → Run workflow**
2. Optionally enable **dry run** to preview the release without creating
   anything
3. Click **Run workflow**

### What the Workflow Does

The workflow runs the following steps automatically:

1. **Releasability checks** — builds the solution, runs all tests (including
   public API approval tests), and reviews changed source files for critical
   issues (security vulnerabilities, null reference risks, unhandled
   exceptions, breaking API changes, race conditions, resource leaks). Build
   or test failures and critical code review findings **block** the release.
   Warnings (TODO/FIXME markers, vulnerable dependencies, medium/low code
   review findings) are noted in the review issue but do not block.

2. **Change analysis** — identifies all commits and PRs since the last release
   and categorises them as breaking changes, new features, bug fixes, or other.

3. **Version selection** — applies semantic versioning rules to determine the
   correct version increment (MAJOR, MINOR, or PATCH).

4. **Release notes** — generates structured markdown release notes.

5. **Stale draft cleanup** — deletes any existing draft releases to prevent
   accumulation.

6. **Draft release creation** — creates a new draft GitHub release with the
   selected version, tag, and release notes.

7. **Review issue** — opens a `[Release Review]` issue summarising the proposed
   release, releasability results, change summary, full release notes, and
   approval instructions.

### Concurrency & Cleanup

- **Concurrency control**: If a newer Create Release run starts while an older
  one is still running, the older run is automatically cancelled.
- **Stale draft cleanup**: Before creating a new draft release, the workflow
  deletes any existing draft releases so they don't pile up.

### If the Release Is Blocked

If releasability checks fail, the workflow creates a
`[Release Review] Release Blocked — <reason>` issue listing all failures and
stops. No draft release is created. Fix the issues and re-run the workflow.

## Approval Flow

Once the workflow creates a draft release and review issue:

### Option A — Approve via Label (Recommended)

1. Review the draft release and the `[Release Review]` issue
2. Add the **`release-approved`** label to the review issue
3. The `approve-release.yml` workflow automatically:
   - Publishes the draft release
   - Comments on and closes the review issue
4. Publishing triggers the `Publish NuGet Package` workflow, which builds,
   packs, and publishes both `Augustus.AI` and `Augustus.AI.Reqnroll` to
   NuGet.org

### Option B — Publish Manually

1. Go to **Releases** and open the draft release
2. Edit release notes if needed
3. Click **Publish release**
4. Manually close the review issue

### Cancelling a Release

1. Delete the draft release from the Releases page
2. Close the review issue

## Version Numbering

Augustus follows Semantic Versioning 2.0.0:

- **Major.Minor.Patch** for stable releases
  - Example: `v1.0.0`, `v1.2.3`

- **0.Minor.Patch** for pre-1.0 versions
  - Example: `v0.1.0`, `v0.2.0`

- **Prerelease Suffixes** for pre-release versions
  - `-alpha`: Early preview, may have breaking changes
  - `-beta`: Feature complete, testing phase
  - `-rc.N`: Release candidate (rc.1, rc.2, etc.)
  - Example: `v0.1.0-alpha`, `v1.0.0-beta`, `v1.0.0-rc.1`

## Verifying a Release

After the release is published:

1. Check the `Publish NuGet Package` workflow in GitHub Actions
2. Visit https://www.nuget.org/packages/Augustus.AI to verify the package
3. NuGet.org may take a few minutes to index new packages

## Troubleshooting

### Package not appearing on NuGet.org

- **Check API key**: Verify `NUGET_API_KEY` secret is configured correctly
- **Check version format**: Ensure version matches SemVer format
- **Check for duplicates**: NuGet skips duplicate versions
- **Wait for indexing**: NuGet.org may take a few minutes to index new packages

### Create Release workflow failed

- **Releasability blocked**: Check the `[Release Review] Release Blocked` issue
  for details on what failed (build errors, test failures, critical code review
  findings)
- **Agent timeout**: The workflow has a 30-minute timeout. If analysis is
  taking too long, check for issues with the repository or GitHub API
- Check GitHub Actions logs for detailed error information

### Publish NuGet workflow failed

- Check GitHub Actions logs: https://github.com/chrisjainsley/augustus/actions
- Common issues:
  - Missing or invalid API key
  - Malformed version tag (must start with `v`)
  - Build failures (check build logs)

### Rolling back a release

If a version is published incorrectly:

1. Delete the GitHub release
2. Delete the version from NuGet.org (if possible — contact support)
3. Re-publish with a patched version number (e.g., `v0.2.1`)

## Frequently Asked Questions

**Q: Can I release from any branch?**
A: The workflow triggers on `workflow_dispatch` from the Actions tab. Always
ensure `master` is up-to-date before triggering, as the workflow checks out
the default branch.

**Q: What if I need to release a hotfix?**
A: Create a bugfix branch, fix the issue, merge to `master`. Then trigger the
Create Release workflow from the Actions tab.

**Q: Do I need to manually update the version in the csproj file?**
A: No. The publish workflow automatically extracts the version from the release
tag and updates the csproj file.

**Q: What happens if I trigger multiple releases at once?**
A: The workflow has concurrency control — newer runs cancel older in-progress
runs. Stale draft releases are automatically cleaned up before creating a new
one.

**Q: How does the code review work?**
A: The agent reviews all `.cs` files changed since the last release for critical
issues (security vulnerabilities, null references, empty catch blocks, breaking
API changes, race conditions, resource leaks). Only critical findings block the
release — medium and low findings are included as warnings in the review issue.

**Q: What if the `release-approved` label doesn't exist?**
A: Create it with:
```bash
gh label create "release-approved" --color "0E8A16" --description "Approves a draft release for publishing"
```
