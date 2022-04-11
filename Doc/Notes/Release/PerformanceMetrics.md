# Weevil: Performance Metrics

The following is a highlevel comparison of how Weevil performs.  Context:
- Build: `Debug`
- Visual Studio Debugger Attached: `No`

| Scenario                   | Version | Log File         | Memory Usage       | Load Time (disk+filter) | `#Summary` filter |
| -------------------------- | ------- | ---------------- | ------------------ | ----------------------- | ----------------- |
| Weevil .Net 4.8            | 2.10.0  | 1million records | 1,463MB to 1,463MB | 3.5s + 0.7s = 4.2s      | 0.810s            |
| Weevil .Net 4.8            | 2.10.0  | 7million records | 4,073MB to 4,073MB | 22.2s + 5.3s = 27.5     | 6.6s              |
| Weevil /wo Material Design | 2.5.0   | 1million records | 1,500MB to 1,700MB | 1.5s (A)                | 1.6s              |
| Weevil /w Material Design  | 2.5.0   | 1million records | 1,700MB to 1,800MB | 1.6s (A)                | 1.6s              |
| Weevil /wo Material Design | 2.5.0   | 7million records | 5,300MB to 5,400MB | 10.9s (A)               | 11.6s             |
| Weevil /w Material Design  | 2.5.0   | 7million records | 4,800MB to 5,350MB | 11.4s (A)               | 11.6s             |

(A) During the development of 2.10.0 a bug was found in how the file load time was measured. These values may not be accurate.

## Appendices

### Appendix A: Additional Reading

- [6 Best Practices to Keep a .NET Application’s Memory Healthy][A]

[A]: https://michaelscodingspot.com/application-memory-health/