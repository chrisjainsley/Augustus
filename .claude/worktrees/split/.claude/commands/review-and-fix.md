# Code Review and Auto-Fix Command

You are performing an automated code review and fix cycle for the Augustus project.

## Your Task

1. **Run Comprehensive Code Review**
   - Review all C# source files in `Augustus/Augustus/` and `Augustus/Augustus.Tests/`
   - Check for:
     - Code quality issues (naming, readability, maintainability)
     - Potential bugs or edge cases
     - Missing error handling
     - Performance issues
     - Security vulnerabilities
     - Inconsistencies with .NET best practices
     - Missing XML documentation
     - Async/await anti-patterns
     - Resource disposal issues
     - Test coverage gaps

2. **Prioritize Issues**
   - Critical: Security, bugs, crashes
   - High: Performance, resource leaks, correctness
   - Medium: Code quality, maintainability
   - Low: Style, minor optimizations

3. **Implement Fixes**
   - Fix all Critical and High priority issues
   - Fix Medium priority issues if straightforward
   - Document what you're fixing and why
   - Make targeted, minimal changes
   - Preserve existing functionality

4. **Run Tests**
   - Build the project: `dotnet build Augustus/Augustus/Augustus.csproj`
   - Run all tests: `dotnet test Augustus/Augustus.Tests/Augustus.Tests.csproj`
   - If tests fail, diagnose and fix
   - Ensure all tests pass before reporting completion

5. **Report Results**
   - Summarize issues found and fixed
   - List any remaining issues (Low priority)
   - Show test results
   - Provide recommendations for future improvements

## Important Notes

- Be thorough but pragmatic
- Don't over-engineer or make unnecessary changes
- Maintain backward compatibility
- Keep the existing architecture and patterns
- Only fix real issues, not style preferences
- Update tests if you change public APIs
