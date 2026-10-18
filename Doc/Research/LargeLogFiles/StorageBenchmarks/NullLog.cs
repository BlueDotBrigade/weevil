// Stand-in for Weevil's Diagnostics.Log, which the copied parsers in WeevilCopies/ call.
// The parsers only log for malformed input and end of file. Messages are discarded,
// as Weevil's default log writer does in Release builds.

namespace BlueDotBrigade.Weevil.Diagnostics
{
    internal static class Log
    {
        public static NullLogWriter Default { get; } = new NullLogWriter();
    }

    internal sealed class NullLogWriter
    {
        public void Write(LogSeverityType severity, string message)
        {
            // intentionally discarded
        }
    }
}
