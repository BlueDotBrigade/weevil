# FluentAssertions Compares Records by Line Number

> Notes from #1141, 2026-11-13, measured with FluentAssertions 7.0.0.

## Symptom

Two records on the same line, with different text, timestamps and severity, pass `Should().Be(...)`. A test that seems to check a record's content checks only its line number.

## Cause

Since #1135, records are equal when their line numbers are equal: `Record` overrides `Equals`. FluentAssertions uses `Equals` in most of its assertions:

| Assertion | Two records on line 5, with different content |
|---|---|
| `record.Should().Be(other)` | Passes |
| `records.Should().Equal(others)` | Passes |
| `records.Should().Contain(other)` | Passes |
| `record.Should().BeEquivalentTo(other)`, declared as `Record` | Passes |
| `record.Should().BeEquivalentTo(other)`, declared as `IRecord` | Fails: it compares member by member |

`BeEquivalentTo` uses `Equals` for a type that overrides it, and judges that by the declared type. `IRecord` is an interface, so for it, `BeEquivalentTo` compares each property.

## What to Do

- To check which records a list holds, compare them as records, or compare their line numbers. Both say the same thing.
- To check a record's content, timestamp or severity, assert on that property: `record.Content.Should().Be("...")`.
- Do not rely on `BeEquivalentTo` to compare content: whether it does depends on how the variable is declared.
