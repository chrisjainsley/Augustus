---
name: verify
description: Build the solution and run all tests across target frameworks to verify changes are correct.
---

Build and test the Augustus solution to verify all changes work correctly.

## Steps

1. **Build the solution:**
   ```bash
   dotnet build Augustus/Augustus.sln --configuration Release
   ```
   Fix any build errors before proceeding.

2. **Run all tests:**
   ```bash
   dotnet test Augustus/Augustus.sln --configuration Release --no-build
   ```

3. **Report results:**
   - If all tests pass, report success with test count summary.
   - If any tests fail, diagnose the failures, fix them, and re-run until all pass.

## Notes

- The solution multi-targets net6.0, net7.0, net8.0, net9.0, and net10.0. CI tests all frameworks, but for local verification a single framework run is acceptable unless the change is framework-sensitive.
- For framework-specific testing: `dotnet test Augustus/Augustus.sln -f net9.0`
