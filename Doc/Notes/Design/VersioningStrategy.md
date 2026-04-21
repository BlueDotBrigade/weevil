# Weevil Versioning Strategy

## Semantic Versioning (SemVer)

Format:

```
major.minor.patch.build
```

Example:

```
2.12.0.1523
```

* **Major**: breaking changes
* **Minor**: new features (backwards compatible)
* **Patch**: bug fixes

### Limitation

Over time, the "major" version (e.g., `2`) lost meaning for Weevil, as releases are continuous and not grouped into large breaking changes.

---

## Calendar Versioning (CalVer)

Common formats used in industry:

### Option A — Date-based

```
YYYY.MM.DD.revision
```

Example:

```
2026.04.20.1523
```

* **Day** represents the exact release date
* Works well for high-frequency daily releases

### Option B — Build-based (recommended for Weevil)

```
YYYY.MM.build.revision
```

Example:

```
2026.04.0.1523
```

* **Build** represents release iteration within the month
* `0` = first release
* `1+` = hotfixes or follow-up releases
* **Revision** = auto-incremented build number (CI)

---

## Weevil Versioning Options

Weevil is evaluating the following **calendar versioning formats**:

### Option A — Build-based (4-digit year)

```
YYYY.MM.build.revision
```

Example:

```
2026.04.0.1523
2026.04.1.1601
```

* `build` represents release iteration within the month
* `0` = first release
* `1+` = hotfixes or follow-up releases
* Groups releases logically by month

---

### Option B — Date-based

```
YYYY.MM.DD.revision
```

Example:

```
2026.04.20.1523
```

* `day` represents the exact release date
* More granular, but less expressive for hotfix relationships

---

### Option C — Short Year Variant (under consideration)

```
yy.M.patch.build
```

Example:

```
26.4.0.1523
26.4.1.1601
```

* `yy` = 2-digit year
* `M` = non-padded month (1–12)
* `patch` = release / hotfix indicator
* `build` = auto-incremented

---

## Additional Considerations

### 1. Two-digit vs Four-digit Year

* `2026` is unambiguous and industry standard
* `26` is shorter but introduces long-term ambiguity
* Tools, sorting, and logs are clearer with `YYYY`

👉 Recommendation: prefer **4-digit year (`YYYY`)**

---

### 2. Month Padding

* `.NET Version` treats segments as integers (no inherent padding)
* Display formatting would need to handle `04` vs `4`

👉 Recommendation: store as integer (`4`), format as needed in UI

---

### 3. Patch / Hotfix Semantics Problem

Example scenario:

* April 29 → Release (`.4.0`)
* April 30 → Bug found
* May 1 → Hotfix released

Result:

```
26.4.0 → 26.5.0
```

Issue:

* The "patch" number resets
* The hotfix relationship is **lost across month boundaries**

This makes `patch` misleading, as it no longer strictly represents a fix to a previous version.

---

## Evaluation Summary

| Format                 | Strengths                        | Weaknesses                          |
| ---------------------- | -------------------------------- | ----------------------------------- |
| YYYY.MM.build.revision | Clear, scalable, hotfix-friendly | No exact date                       |
| YYYY.MM.DD.revision    | Precise date                     | Weak hotfix grouping                |
| yy.M.patch.build       | Short, simple                    | Ambiguous year, patch inconsistency |

---

## Recommended Direction

The **best fit for Weevil** is:

```
YYYY.MM.build.revision
```

Example:

```
2026.4.0.1523
2026.4.1.1601
2026.5.0.1700
```

### Why this is the best fit

* Avoids ambiguous 2-digit year
* Avoids misleading "patch" semantics across months
* Cleanly represents hotfix sequencing within a release window
* Aligns naturally with `.NET Version (major.minor.build.revision)`
* Keeps version numbers readable and predictable

---

## Direction (Under Evaluation)

Weevil is moving away from Semantic Versioning due to the loss of meaning in the "major" version and the shift toward continuous delivery.

Calendar Versioning is the preferred direction, with **YYYY.MM.build.revision** currently the leading candidate based on clarity, simplicity, and correctness.

A final decision will be made before the next release.

---

## Compatibility Considerations

Semantic Versioning uses the **major version** to signal potential breaking changes:

```
2.12.0 → 3.0.0  (possible breaking changes)
```

With Calendar Versioning, this signal is no longer implicit. For example:

```
2026.04.0 → 2026.05.0
```

These versions do not indicate whether breaking changes were introduced.

### Weevil Approach

Weevil does not rely on the version number alone to communicate compatibility.

Instead:

* Breaking changes must be **explicitly documented** in release notes
* Backwards compatibility should be **preserved where possible**
* Behavioral changes must be **clearly called out**

### Rationale

* Weevil evolves continuously rather than in large "major" releases
* The previous "major version" no longer reliably indicated breaking changes
* Explicit communication is more reliable than implicit version signals

---

## Notes

* Month must be **zero-padded** (e.g., `04`, not `4`) to ensure correct sorting
* Full year (`2026`) is used to avoid ambiguity
* Build number is system-generated and ensures uniqueness
* Version format aligns with .NET: `major.minor.build.revision`

