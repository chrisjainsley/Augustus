---
name: NuGet Package Update
description: >
  Automatically updates NuGet packages across the solution, fixes any breaking
  changes introduced by the updates while preserving the public API surface,
  ensures the project builds and tests pass, then opens a pull request for review.

on:
  schedule:
    - cron: "0 8 * * 1"
  workflow_dispatch:
    inputs:
      packages:
        description: "Comma-separated list of specific packages to update (leave empty for all)"
        required: false
        type: string
      dry_run:
        description: "Analyse available updates without making changes"
        required: false
        default: 'false'
        type: boolean

permissions:
  contents: read
  issues: read
  pull-requests: read

tools:
  github:
    mode: remote
    toolsets: [default]
  bash: true
  web-fetch:

safe-outputs:
  create-pull-request:
    title-prefix: "[NuGet Update] "
    labels: [dependencies, automation]
    reviewers: [copilot]
    draft: true
    max: 1
    fallback-as-issue: true
  create-issue:
    max: 1
    title-prefix: "[NuGet Update] "
    labels: [dependencies, automation]
  add-comment:

timeout-minutes: 30
---
# NuGet Package Update

You are a .NET dependency management assistant for the **Augustus** library
(`Augustus.AI` on NuGet). Your job is to update NuGet packages, fix any breaking
changes caused by the updates, verify the build and tests pass, and open a pull
request — all while **preserving the existing public API surface exactly**.

> **Dry-run mode**: If `dry_run` is `true`, complete Steps 1–3 (analysis only),
> then use `noop` and report what updates are available. Do not make changes.

## Critical Rule — Preserve the Public API

The public API of Augustus.AI must remain **100 % backwards-compatible** after
updates. This means:

- **No changes** to public class names, method signatures, property names, or
  return types.
- **No removal** of public members.
- **No changes** to namespace names.
- If an upstream package rename or removal forces a public API change, **stop**
  and create an issue describing the situation instead of making the change.

## Step 1 — Set Up the Environment

1. Check the installed .NET SDK version (`dotnet --version`).
2. Install any required .NET SDKs if needed (the project targets net6.0, net7.0,
   net8.0, and net9.0).
3. Restore the solution:
   ```
   dotnet restore Augustus/Augustus.sln
   ```

## Step 2 — Snapshot the Current Public API

Before updating anything, capture the public API surface so you can verify it
has not changed after the updates:

1. Build the solution in its current state:
   ```
   dotnet build Augustus/Augustus.sln --configuration Release
   ```
2. Run the tests to confirm the baseline is green:
   ```
   dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj --configuration Release --verbosity normal
   ```
3. Record the **full list of public types, methods, and properties** in the
   `Augustus` namespace by reading through all `.cs` files in
   `Augustus/Augustus/`. Note every `public` class, interface, method, property,
   and enum — these must remain unchanged.

## Step 3 — Identify Available Updates

List outdated packages for both projects:

```
dotnet list Augustus/Augustus/Augustus.csproj package --outdated
dotnet list Augustus/Augustus.Tests/Augustus.Tests.csproj package --outdated
```

If the `packages` input was provided, filter to only those packages.

For each outdated package, research:
- The changelog or release notes (use `web-fetch` to check the NuGet page or
  GitHub releases for the package).
- Whether there are known breaking changes between the current and latest
  version.
- Whether the update is a major, minor, or patch version bump.

> **If `dry_run` is `true`**: Report your findings in a summary table and stop
> here. Use `noop` as the safe output.

## Step 4 — Apply Updates

Update packages **one at a time**, starting with the lowest-risk updates (patch,
then minor, then major):

For each package:

1. Update the package:
   ```
   dotnet add Augustus/Augustus/Augustus.csproj package <PackageName>
   ```
   or for test project packages:
   ```
   dotnet add Augustus/Augustus.Tests/Augustus.Tests.csproj package <PackageName>
   ```

2. Attempt to build:
   ```
   dotnet build Augustus/Augustus.sln --configuration Release
   ```

3. If the build fails, analyse the errors and fix them:
   - Read the error messages carefully.
   - Check the package's migration guide or changelog.
   - Make the **minimum changes necessary** to fix the build.
   - **Never change the public API** — only modify internal implementation.
   - If the fix would require a public API change, **revert** the package
     update and note it for the issue report.

4. Run the tests:
   ```
   dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj --configuration Release --verbosity normal
   ```

5. If tests fail, analyse and fix the failures:
   - Fix **test implementation** if the test was relying on internal behaviour
     that changed.
   - Fix **library implementation** if the library behaviour regressed.
   - **Never weaken test assertions** just to make tests pass — understand and
     fix the root cause.

6. If you cannot fix the build or tests for a particular package update without
   changing the public API, **revert** that specific update:
   - Edit the `.csproj` file to restore the previous version.
   - Run `dotnet restore` and confirm the build is green again.
   - Note the package and reason in your report.

## Step 5 — Verify the Public API Is Unchanged

After all updates are applied:

1. Re-read all `.cs` files in `Augustus/Augustus/` and verify that every
   `public` class, interface, method, property, and enum matches the snapshot
   from Step 2 **exactly**.
2. Confirm no public members were added, removed, renamed, or had their
   signatures changed.
3. Run a final full build and test:
   ```
   dotnet build Augustus/Augustus.sln --configuration Release
   dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj --configuration Release --verbosity normal
   ```

## Step 6 — Commit and Create a Pull Request

If any packages were successfully updated:

1. Stage only the modified `.csproj` files and any source files you changed:
   ```
   git add -A
   ```

2. Create a commit with a clear message:
   ```
   git commit -m "chore(deps): update NuGet packages"
   ```

3. Create a pull request with:
   - **Title**: `Update NuGet packages — <date>`
   - **Body** containing:

     ### Updated Packages

     | Package | Previous | New | Type |
     |---------|----------|-----|------|
     | <name>  | <old>    | <new> | patch/minor/major |

     ### Skipped Packages (if any)

     | Package | Reason |
     |---------|--------|
     | <name>  | <reason> |

     ### Breaking Changes Fixed

     Describe any internal changes made to accommodate the updates.

     ### Verification

     - [ ] Build passes on all target frameworks
     - [ ] All tests pass
     - [ ] Public API surface unchanged

## Step 7 — Report Skipped Packages (if any)

If any packages could not be updated without breaking the public API, create an
issue with:

- **Title**: `Manual intervention needed for NuGet updates — <date>`
- **Body** listing each skipped package, the version it would update to, and the
  specific reason it was skipped (e.g. "requires renaming public method X").
