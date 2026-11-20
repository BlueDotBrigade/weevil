// How .NET's file locks behave, for the lock Weevil keeps beside an open log (#1254). See README.md.
//
//   dotnet run -- table [folder]           every case, in this process
//   dotnet run -- hold [folder] [ms]       holds the lock as Weevil does, for another process to probe
//   dotnet run -- probe [folder]           tries to take the lock, then reads who holds it
using System.Runtime.InteropServices;
using System.Text;

var mode = args.Length > 0 ? args[0] : "table";
var folder = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "LogLockPoC");
Directory.CreateDirectory(folder);
var path = Path.Combine(folder, "App.log.lock");
const FileShare ReadWriteDelete = FileShare.ReadWrite | FileShare.Delete;

switch (mode)
{
    case "table":
        Case("Holder FileShare.Read, write access", () => Open(FileAccess.ReadWrite, FileShare.Read),
            ("Same", () => Open(FileAccess.ReadWrite, FileShare.Read)),
            ("FileShare.None", () => Open(FileAccess.ReadWrite, FileShare.None)),
            ("Read, FileShare.ReadWrite", () => Open(FileAccess.Read, FileShare.ReadWrite)));
        Case("Holder FileShare.None", () => Open(FileAccess.ReadWrite, FileShare.None),
            ("Read, FileShare.ReadWrite", () => Open(FileAccess.Read, FileShare.ReadWrite)));
        Case("Holder FileStream.Lock on a range", () => LockRange(Open(FileAccess.ReadWrite, ReadWriteDelete)),
            ("Lock on the same range", () => LockRange(Open(FileAccess.ReadWrite, ReadWriteDelete))));
        Case("Holder as Weevil holds it", () => Acquire("holder"),
            ("Taken as Weevil takes it", () => Acquire("second")),
            ("Read, FileShare.ReadWrite | Delete", () => ReadOwner()));
        using (Acquire("disposed")) { }
        Console.WriteLine($"Disposed: the file is {(File.Exists(path) ? "still there" : "deleted")}");
        File.WriteAllText(path, "User=crashed\n");
        Console.WriteLine($"A file left behind, no one holding it: {Try(() => Acquire("next"))}");
        break;

    case "hold":
        using (Acquire("holder"))
        {
            Console.WriteLine("Holding");
            Thread.Sleep(args.Length > 2 ? int.Parse(args[2]) : 60_000);
        }
        break;

    case "probe":
        Console.WriteLine($"Exists: {File.Exists(path)}");
        Console.WriteLine($"Taken as Weevil takes it: {Try(() => Acquire("prober"))}");
        Console.WriteLine($"Read: {Try(() => ReadOwner())}");
        break;
}

void Case(string holder, Func<IDisposable> hold, params (string Name, Func<IDisposable> Open)[] others)
{
    using IDisposable held = hold();
    foreach (var (name, open) in others)
    {
        Console.WriteLine($"{holder} | {name} | {Try(open)}");
    }
}

FileStream Open(FileAccess access, FileShare share, FileOptions options = FileOptions.None) =>
    new FileStream(path, FileMode.OpenOrCreate, access, share, bufferSize: 0, options);

IDisposable LockRange(FileStream stream)
{
    try
    {
        stream.Lock(1L << 40, 1);
        return stream;
    }
    catch
    {
        stream.Dispose();
        throw;
    }
}

// As LogLock: on Windows, write access sharing only reading; elsewhere, exclusive, then made shared.
FileStream Acquire(string who)
{
    if (OperatingSystem.IsWindows())
    {
        return Write(Open(FileAccess.ReadWrite, FileShare.Read, FileOptions.DeleteOnClose), who);
    }

    FileStream stream = Open(FileAccess.ReadWrite, FileShare.None, FileOptions.DeleteOnClose);
    if (Native.Flock(stream.SafeFileHandle.DangerousGetHandle().ToInt32(), 1 | 4) != 0)
    {
        Console.WriteLine($"flock failed: {Marshal.GetLastPInvokeError()}");
    }

    return Write(stream, who);
}

FileStream Write(FileStream stream, string who)
{
    stream.SetLength(0);
    stream.Write(Encoding.UTF8.GetBytes($"User={who}\n"));
    stream.Flush();
    return stream;
}

IDisposable ReadOwner()
{
    var stream = new FileStream(path, FileMode.Open, FileAccess.Read, ReadWriteDelete);
    Console.WriteLine($"  read: {new StreamReader(stream).ReadToEnd().Trim()}");
    return stream;
}

static string Try(Func<IDisposable> open)
{
    try
    {
        open().Dispose();
        return "Opens";
    }
    catch (Exception exception)
    {
        return $"{exception.GetType().Name}, HResult 0x{exception.HResult:X8}";
    }
}

static class Native
{
    [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
    public static extern int Flock(int fileDescriptor, int operation);
}
