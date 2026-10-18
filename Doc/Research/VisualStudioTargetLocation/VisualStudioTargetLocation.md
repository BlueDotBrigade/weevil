# Visual Studio 2026 Target Location Display Strategy

## Objective
Given either:
1. a fully qualified method name, or
2. a file path + line number,

display a "target location" in a form that is simple for users and reliable in Visual Studio 2026.

## Recommendation (single strategy)
Use **Visual Studio diagnostic location format** as the canonical display format:

`<absolute-file-path>(<line>,1)`

Examples:
- `C:\repo\Src\Foo\Bar.cs(128,1)`
- `D:\work\MyApp\Program.cs(42,1)`

### Why this is the best default
1. **Clickable in Visual Studio surfaces** that parse diagnostic-style text (Output/Build-like panes).
2. **Human readable** even when not clickable.
3. **Stable and well-known** format with minimal ambiguity.
4. Works directly when input is already `FilePath+LineNumber`.

## Input handling rules

### Case A: Input is `FilePath+LineNumber`
- Normalize to absolute path if possible.
- Clamp line number to `>= 1`.
- Render as `<absolute-file-path>(<line>,1)`.

### Case B: Input is fully qualified method name
- Attempt symbol resolution against the loaded solution/workspace.
- If resolved to source location, render canonical diagnostic location:
  - `<absolute-file-path>(<line>,1)`
- If symbol resolves to metadata/decompiled/binary only, fall back to:
  - `Namespace.Type.Method(...)` (non-clickable symbol fallback)
  - Optional hint: `Use Ctrl+T (Go to All) in Visual Studio and paste the symbol.`

## Reliability notes
- Prefer source-backed file location over symbol text whenever both exist.
- Do not emit relative paths as canonical output.
- Use `,1` for column to avoid parser ambiguity.
- If multiple definitions are found, choose deterministic first result (e.g., primary declaration), and include a short note that multiple matches exist.

## Future implementation guidance for issue #252
1. Build a small formatter utility with two entry points:
   - `FormatFromPathAndLine(path, line)`
   - `FormatFromFullyQualifiedMethodName(symbol)`
2. Return a result object:
   - `DisplayText` (always present)
   - `IsClickableLikely` (`true` when file location exists)
   - `ResolutionNotes` (optional)
3. Keep this feature independent from the Weevil core behavior; it should be a presentation/helper capability.

## Acceptance criteria
1. Given `C:\x\y\z.cs` + `77`, output exactly `C:\x\y\z.cs(77,1)`.
2. Given symbol `A.B.C.DoWork`, when source exists at line 210 in `C:\src\C.cs`, output `C:\src\C.cs(210,1)`.
3. Given symbol without source mapping, output symbol text and navigation hint.
4. Output must be deterministic for repeated runs on same workspace state.
