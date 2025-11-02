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

## Release Workflow

### Step 1: Prepare the Release

Ensure all changes are committed to the `master` branch:

```bash
git checkout master
git pull origin master
```

### Step 2: Create a GitHub Release

The automated pipeline is triggered when you **create a GitHub release** (not just a tag).

#### Using GitHub Web UI:

1. Go to https://github.com/chrisjainsley/augustus/releases
2. Click "Draft a new release"
3. Click "Choose a tag"
4. Enter the version tag in format: `v{major}.{minor}.{patch}[-suffix]`
   - Examples: `v0.2.0`, `v1.0.0`, `v0.1.0-beta`, `v1.0.0-rc.1`
5. Set release title (e.g., "Augustus 0.2.0")
6. Add release notes/changelog
7. Mark as "Pre-release" if applicable (for alpha, beta, rc versions)
8. Click "Publish release"

#### Using GitHub CLI:

```bash
# For stable releases
gh release create v0.2.0 -t "Augustus 0.2.0" -n "Release notes here"

# For prerelease versions
gh release create v0.1.0-alpha -t "Augustus 0.1.0 Alpha" -n "Early preview" --prerelease
```

### Step 3: Automated Publishing

Once you publish the release:

1. GitHub Actions automatically triggers the `Publish NuGet Package` workflow
2. The workflow:
   - Extracts the version from the tag (removes 'v' prefix)
   - Updates `Augustus/Augustus/Augustus.csproj` with the new version
   - Builds the project for all target frameworks (net6.0, net7.0, net8.0, net9.0, net10.0)
   - Creates NuGet packages (`.nupkg` and `.snupkg` symbol package)
   - Publishes to NuGet.org
   - Attaches packages to the GitHub release

3. Monitor progress at: https://github.com/chrisjainsley/augustus/actions

### Step 4: Verify Publication

After the workflow completes:

1. Check GitHub Actions logs for any errors
2. Visit https://www.nuget.org/packages/Augustus.AI to verify the package
3. Search for the new version number to confirm it's published

## Version Numbering

Augustus follows Semantic Versioning 2.0.0:

- **Major.Minor.Patch** for stable releases
  - Example: `v1.0.0`, `v1.2.3`
  - Use when API is stable and ready for production

- **0.Major.Minor** for pre-release versions
  - Example: `v0.1.0`, `v0.2.0`
  - Use for versions before 1.0.0 release

- **Prerelease Suffixes** for pre-release versions
  - `-alpha`: Early preview, may have breaking changes
  - `-beta`: Feature complete, testing phase
  - `-rc.N`: Release candidate (rc.1, rc.2, etc.)
  - Example: `v0.1.0-alpha`, `v1.0.0-beta`, `v1.0.0-rc.1`

## Examples

### Releasing version 0.2.0

```bash
git checkout master
git pull origin master

# Using GitHub CLI
gh release create v0.2.0 \
  -t "Augustus 0.2.0" \
  -n "## Features
- Added GPT-5 model support
- Improved caching performance

## Bug Fixes
- Fixed stream seeking issue in non-seekable streams"
```

### Releasing an alpha version

```bash
# Using GitHub CLI
gh release create v0.1.0-alpha \
  -t "Augustus 0.1.0 Alpha" \
  -n "Early preview of Augustus" \
  --prerelease
```

## Troubleshooting

### Package not appearing on NuGet.org

- **Check API key**: Verify `NUGET_API_KEY` secret is configured correctly
- **Check version format**: Ensure version matches SemVer format
- **Check for duplicates**: NuGet skips duplicate versions (use `--skip-duplicate` flag)
- **Wait for indexing**: NuGet.org may take a few minutes to index new packages

### Workflow failed

- Check GitHub Actions logs: https://github.com/chrisjainsley/augustus/actions
- Common issues:
  - Missing or invalid API key
  - Malformed version tag (must start with 'v')
  - Build failures (check build logs)
  - Missing GitHub token (should be automatic)

### Rolling back a release

If a version is published incorrectly:

1. Delete the GitHub release
2. Delete the version from NuGet.org (if possible - contact support)
3. Re-publish with a patched version number (e.g., v0.2.1)

## Frequently Asked Questions

**Q: Can I release from any branch?**
A: The workflow triggers on release creation. Always ensure `master` is up-to-date before creating a release, as the workflow checks out the tag commit.

**Q: What if I need to release a hotfix?**
A: Create a bugfix branch, fix the issue, commit, and merge to `master`. Then create a release tag (e.g., v0.1.1).

**Q: Do I need to manually update the version in the csproj file?**
A: No! The workflow automatically extracts the version from your release tag and updates the csproj file.

**Q: Can I create multiple releases at once?**
A: Yes, but each release will be processed by the workflow sequentially. There's no conflict between parallel releases.

**Q: How do I update version history on NuGet.org?**
A: You can edit release notes on NuGet.org, but the actual version is locked. For changes, create a new patch release (e.g., v0.2.1).
