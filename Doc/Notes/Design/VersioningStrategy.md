# Weevil Versioning Strategy (Current)

## TL;DR

Weevil uses a centralized, deterministic-friendly versioning model for SDK-style .NET projects:

- Base release line: `2.12.0`
- `AssemblyVersion`: `2.12.0.0` (stable)
- `FileVersion`: `2.12.0.<revision>` (unique per build)
- `InformationalVersion`: `2.12.0+<Configuration>.<revision>`

All shared version properties are defined once in `Directory.Build.props`.

---

## Background and Challenges

### 1) Wildcards vs .NET version attributes

In C#, wildcard `*` is historically supported only for `AssemblyVersion` in source attributes (for build/revision parts).

- `AssemblyVersion("1.2.*")`: supported by compiler (legacy behavior)
- `AssemblyFileVersion("1.2.*")`: not supported
- `AssemblyInformationalVersion("...")`: free-form string, no wildcard expansion behavior

In SDK-style projects, wildcard-based assembly versioning is not the recommended path.

### 2) Deterministic builds

Deterministic builds are a .NET SDK best practice and are expected in modern CI/CD.

Key point: wildcard assembly version auto-generation conflicts with deterministic builds (compiler error scenario, e.g. CS8357).

Therefore, Weevil keeps deterministic builds enabled and uses explicit version properties.

### 3) Need for unique build identity

Unique build identity is still important for:

- diagnostics and support
- artifact traceability
- release verification

We achieve uniqueness via the **revision** segment in `FileVersion` and `InformationalVersion`.

### 4) Manageability

The base release line (e.g., `2.12.0`) must be updateable in one place.

Weevil centralizes this in:

- repo root: `Directory.Build.props`

---

## Industry-Standard Guidance Applied

We align with common .NET practices:

1. Use SDK-style generated assembly metadata (no manual `AssemblyInfo.cs` version duplication).
2. Keep `AssemblyVersion` stable unless a binding-level change is needed.
3. Put per-build uniqueness in `FileVersion` / `InformationalVersion`.
4. Keep deterministic builds enabled.
5. Drive unique revision from CI build numbers when available.

---

## High-Level Implementation

### Central version properties

`Directory.Build.props` defines:

- `WeevilReleaseVersion = 2.12.0`
- `VersionPrefix = $(WeevilReleaseVersion)`
- `AssemblyVersion = $(VersionPrefix).0`
- `FileVersion = $(VersionPrefix).$(BuildRevision)`
- `InformationalVersion = $(VersionPrefix)+$(Configuration).$(BuildRevision)`

Including `$(Configuration)` (`Debug` / `Release`) in `InformationalVersion` is intentional.
It prevents ambiguity when diagnosing builds from logs, crash reports, and local artifacts.

The `+...` portion follows **Semantic Versioning 2.0.0 build metadata** conventions (for example, `2.12.0+Release.1452`).
This metadata is useful for traceability and does not change the core release identity (`2.12.0`).

### Revision source

`BuildRevision` is derived from:

1. `BUILD_BUILDID` (if present)
2. `GITHUB_RUN_NUMBER` (if present)
3. local fallback (time-based), then reduced to valid 16-bit revision range

This ensures a revision is always available and valid for .NET version fields.

### Import behavior across folders

MSBuild auto-imports the nearest `Directory.Build.props`.

Because Weevil has both:

- `Directory.Build.props` (root)
- `Src/Directory.Build.props`

`Src/Directory.Build.props` explicitly imports `..\Directory.Build.props` so source projects inherit shared version settings.

---

## Operational Notes for Developers

- To start a new release line, update only `WeevilReleaseVersion` at repo root.
- Prefer CI-provided build numbers for guaranteed monotonic revision values.
- Avoid reintroducing manual assembly version attributes in project `AssemblyInfo.cs`.
- Keep deterministic build settings on for SDK-style projects.

---

## Appendix A: Additional Reading

- [Semantic Versioning 2.0.0](https://semver.org/)
- [CalVer](https://calver.org/)
- [.NET assembly versioning overview](https://learn.microsoft.com/dotnet/standard/assembly/versioning)
- [Set assembly attributes in SDK-style project files](https://learn.microsoft.com/dotnet/standard/assembly/set-attributes-project-file)
- [C# deterministic compiler option](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/code-generation#deterministic)
- [MSBuild reference for .NET SDK projects (assembly attribute properties)](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#assembly-attribute-properties)

---

## Appendix B: Semantic vs Calendar Versioning

### Option 1: Semantic Versioning (SemVer)

Format example:

```
2.12.0
2.12.0.1452   (when a revision/build segment is needed for file metadata)
```

#### Pros

- Widely recognized industry standard for libraries and APIs.
- Communicates intent well (`major` breaking change, `minor` feature, `patch` fix).
- Works naturally with package ecosystems and dependency ranges.
- Easy for humans to scan and compare release significance.

#### Cons

- Requires discipline to increment correctly.
- In fast-moving products, strict semantic meaning can drift over time.
- By itself, does not encode build uniqueness unless a separate revision/build component is added.

### Option 2: Calendar Versioning (CalVer)

Format example:

```
yy.MM.dd.revision
26.05.04.1452
```

#### Pros

- Immediate release-date signal.
- Useful for frequent release cadences.
- Naturally supports “what changed when” conversations.

#### Cons

- Version numbers can feel less meaningful about compatibility impact.
- Can be awkward for sorting if formatting is inconsistent:
  - Good: `yy.MM.dd` with zero-padding (lexical sort works).
  - Risky: non-padded dates (for example `26.5.4`) sort oddly.
- Can appear unusual to users expecting SemVer-style compatibility semantics.

### Which is best for Weevil?

Current recommendation: keep **SemVer release line** (`2.12.0`) plus **revision-based build uniqueness** in file/informational metadata.

Why:

- Matches current project direction (consistency was explicitly chosen in #784).
- Reduces churn in release/process docs.
- Preserves common ecosystem expectations while still supporting unique builds.

### If Weevil started today, from scratch

I would still likely choose the same overall direction for Weevil’s current distribution model:

- Stable SemVer for release identity and compatibility communication.
- Revision/build number for per-build uniqueness.

If Weevil were shipping very frequent date-driven product drops (for example, nightly public releases), CalVer could also be a strong choice. In that case, strict zero-padding and clear compatibility notes would be mandatory.