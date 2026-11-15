// Checks the proof of concept's folders, zips, filters and sidecars, without a window, on any operating system (#1157).
// Exit code 0 when every check passes.

using System.IO.Compression;
using System.Text;
using BlueDotBrigade.Windows.Dialogs.Storage;
using OpenAsDialogPoC;

var failures = 0;
var checks = 0;

void Check(bool condition, string what)
{
	checks++;

	if (!condition)
	{
		failures++;
		Console.WriteLine($"FAIL  {what}");
	}
	else
	{
		Console.WriteLine($"pass  {what}");
	}
}

void Add(ZipArchive zip, string name, byte[] content = null)
{
	ZipArchiveEntry entry = zip.CreateEntry(name);

	if (content is not null)
	{
		using Stream stream = entry.Open();
		stream.Write(content);
	}
}

byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

string Names(ListingResult listing) => string.Join(", ", listing.Items.Select(item => $"{item.Name}:{item.Kind}").OrderBy(name => name, StringComparer.Ordinal));

const string SidecarXml = """
	<?xml version="1.0" encoding="utf-8"?>
	<WeevilSidecar xmlns="http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2">
	  <CommonData>
	    <Regions>
	      <Region><Name>Crash</Name><Minimum><ByteOffset>10</ByteOffset><LineNumber>40</LineNumber></Minimum><Maximum><ByteOffset>99</ByteOffset><LineNumber>45</LineNumber></Maximum></Region>
	    </Regions>
	    <TableOfContents>
	      <Section><Level>1</Level><Name>Start</Name><RelatesTo><ByteOffset>0</ByteOffset><LineNumber>1</LineNumber></RelatesTo></Section>
	      <Section><Level>2</Level><Name>Login</Name><RelatesTo><ByteOffset>50</ByteOffset><LineNumber>20</LineNumber></RelatesTo></Section>
	      <Section><Level>2</Level><Name>Upload</Name><RelatesTo><ByteOffset>90</ByteOffset><LineNumber>30</LineNumber></RelatesTo></Section>
	      <Section><Level>1</Level><Name>Shutdown</Name><RelatesTo><ByteOffset>200</ByteOffset><LineNumber>100</LineNumber></RelatesTo></Section>
	    </TableOfContents>
	  </CommonData>
	</WeevilSidecar>
	""";

var root = Directory.CreateTempSubdirectory("OpenAsDialogPoC.SelfTest.").FullName;
var reader = new ContainerReader();

try
{
	// A folder of logs, with a sidecar and an index for one of them, a rolled log, and a zip of logs that holds a zip of its own.
	var logs = Directory.CreateDirectory(Path.Combine(root, "Logs")).FullName;
	File.WriteAllText(Path.Combine(logs, "App.log"), "line 1\n");
	File.WriteAllText(Path.Combine(logs, "App.log.xml"), SidecarXml);
	File.WriteAllText(Path.Combine(logs, "App.log.idx"), "index");
	File.WriteAllText(Path.Combine(logs, "App.log.1"), "rolled\n");
	File.WriteAllText(Path.Combine(logs, "Notes.txt"), "notes");
	Directory.CreateDirectory(Path.Combine(logs, "Archive"));

	byte[] innerZip;
	using (var memory = new MemoryStream())
	{
		using (var zip = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
		{
			Add(zip, "Old.log", Bytes("old log\n"));
			Add(zip, "Old.log.xml", Bytes(SidecarXml));
		}

		innerZip = memory.ToArray();
	}

	var zipPath = Path.Combine(logs, "Bundle.zip");
	using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
	{
		Add(zip, "2026/");
		Add(zip, "2026/Server.log", Bytes("server log\n"));
		Add(zip, "2026/Server.log.xml", Bytes(SidecarXml));
		Add(zip, "2026/Server.log.idx", Bytes("idx"));
		Add(zip, @"Windows\Client.log", Bytes("client log\n"));
		Add(zip, "Nested/Inner.zip", innerZip);
		Add(zip, "Readme.txt", Bytes("readme"));
	}

	File.WriteAllText(Path.Combine(logs, "Broken.zip"), "not a zip");

	// Folders on disk.
	var folder = reader.List(Location.ForFileSystem(logs));
	Check(folder.Error is null, "a folder on disk is listed");
	Check(Names(folder) == "App.log.1:File, App.log.idx:File, App.log.xml:File, App.log:File, Archive:Folder, Broken.zip:Container, Bundle.zip:Container, Notes.txt:File",
		$"a folder lists its files, folders and zips: {Names(folder)}");
	Check(reader.List(Location.ForFileSystem(Path.Combine(root, "Missing"))).Error == "This folder does not exist.", "a missing folder says so, rather than looking empty");

	// The "Log files" filter.
	var logFiles = new FileTypeFilter("Log files", new[] { "*.log", "*.log.*", "*.txt" }, new[] { "*.xml", "*.idx" });
	var shown = folder.Items.Where(item => !item.IsFile || logFiles.Matches(item.Name)).Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal);
	Check(string.Join(", ", shown) == "App.log, App.log.1, Archive, Broken.zip, Bundle.zip, Notes.txt",
		$"\"Log files\" shows rolled logs and hides sidecars and indexes: {string.Join(", ", shown)}");
	Check(FileTypeFilter.AllFiles.Matches("App.log.xml") && FileTypeFilter.AllFiles.Matches("anything"), "\"All files\" shows every file");
	Check(logFiles.Matches("APP.LOG"), "filters ignore case");

	// A zip, like a folder.
	var bundle = Location.ForArchive(zipPath);
	var top = reader.List(bundle);
	Check(top.Error is null, "a zip is listed without being extracted");
	Check(Names(top) == "2026:Folder, Nested:Folder, Readme.txt:File, Windows:Folder", $"a zip lists its folders, including those known only from paths: {Names(top)}");

	var year = reader.List(top.Items.Single(item => item.Name == "2026").Open);
	Check(Names(year) == "Server.log.idx:File, Server.log.xml:File, Server.log:File", $"a folder in a zip is listed: {Names(year)}");

	var windows = reader.List(top.Items.Single(item => item.Name == "Windows").Open);
	Check(Names(windows) == "Client.log:File", "a path written with backslashes in the zip is still a folder");

	var nested = reader.List(top.Items.Single(item => item.Name == "Nested").Open);
	var inner = nested.Items.Single();
	Check(inner.Kind == ItemKind.Container, "a zip in a zip is a container");

	var innerListing = reader.List(inner.Open);
	Check(Names(innerListing) == "Old.log.xml:File, Old.log:File", $"a zip in a zip is listed: {Names(innerListing)}");

	Check(reader.List(Location.ForArchive(Path.Combine(logs, "Broken.zip"))).Error == "This is not a zip, or it is damaged.", "a damaged zip says so");
	Check(reader.List(Location.ForArchive(zipPath, "NoSuchFolder")).Error == "This folder is not in the zip.", "a folder missing from a zip says so");

	// Where things are, and going up.
	var server = year.Items.Single(item => item.Name == "Server.log");
	var oldLog = innerListing.Items.Single(item => item.Name == "Old.log");
	Check(server.Location.ToString() == Path.Combine(zipPath, "2026", "Server.log"), $"a file in a zip shows as a path: {server.Location}");
	Check(oldLog.Location.ToString() == Path.Combine(zipPath, "Nested", "Inner.zip", "Old.log"), $"a file in a zip in a zip shows as a path: {oldLog.Location}");
	Check(oldLog.Location.Name == "Old.log" && inner.Open.Name == "Inner.zip" && bundle.Name == "Bundle.zip", "names of files and zips");

	var up = new List<string>();
	for (Location location = oldLog.Location.GetParent(); location is not null && !location.IsComputer && up.Count < 10; location = location.GetParent())
	{
		up.Add(location.ToString());
	}

	Check(up.Take(4).SequenceEqual(new[]
		{
			Path.Combine(zipPath, "Nested", "Inner.zip"),
			Path.Combine(zipPath, "Nested"),
			zipPath,
			logs,
		}),
		$"Up climbs out of a zip in a zip, then out of the zip: {string.Join(" > ", up.Take(4))}");

	// Typed paths.
	Check(Location.Parse(oldLog.Location.ToString(), reader.IsContainerName)?.Equals(oldLog.Location) == true, "a typed path into a zip in a zip finds the file");
	Check(Location.Parse($"\"{logs}\"", reader.IsContainerName)?.Equals(Location.ForFileSystem(logs)) == true, "a pasted path in quotes finds the folder");
	Check(Location.Parse(Path.Combine(root, "Nowhere", "x.log"), reader.IsContainerName) is null, "a path to nothing is not found");
	Check(reader.GetKind(oldLog.Location) == ItemKind.File && reader.GetKind(top.Items.Single(item => item.Name == "Nested").Location) == ItemKind.Folder
		&& reader.GetKind(bundle) == ItemKind.Container, "what a typed path names: a file, a folder in a zip, a zip");

	// Companions, such as a log's sidecar, beside it in a zip.
	Location serverSidecar = ContainerReader.GetSibling(server.Location, "Server.log.xml");
	Check(reader.Exists(serverSidecar), "a log's sidecar is found beside it in a zip");
	Check(!reader.Exists(ContainerReader.GetSibling(server.Location, "Server.log.missing")), "a missing companion is not found");

	using (Stream stream = reader.OpenRead(serverSidecar))
	{
		var (sections, regions) = Sidecar.Read(stream);
		Check(sections.Count == 2 && regions.Count == 1, "a sidecar is read straight from the zip, with only its level-1 sections");
		Check(sections[0] == new SidecarSection("Start", 1, 99), $"a level-1 section runs to the next level-1 section, holding its level-2 sections: {sections[0]}");
		Check(sections[1] == new SidecarSection("Shutdown", 100, null), $"the last section runs to the end of the log: {sections[1]}");
		Check(regions[0] == new SidecarRegion("Crash", 40, 45), $"a region: {regions[0]}");
	}

	using (Stream stream = reader.OpenRead(ContainerReader.GetSibling(oldLog.Location, "Old.log.xml")))
	{
		Check(Sidecar.Read(stream).Sections.Count == 2, "a sidecar is read from a zip in a zip");
	}

	// Opening: a file on disk stays where it is; a file in a zip comes out with its companions.
	var companions = new[] { ".xml", ".idx" };
	var onDisk = folder.Items.Single(item => item.Name == "App.log");
	Check(reader.Extract(onDisk.Location, companions, Path.Combine(root, "Unused")) == Path.Combine(logs, "App.log") && !Directory.Exists(Path.Combine(root, "Unused")),
		"a log on disk is opened where it is");

	var extracted = reader.Extract(server.Location, companions, Path.Combine(root, "Extracted"));
	Check(File.ReadAllText(extracted) == "server log\n" && File.Exists(extracted + ".xml") && File.ReadAllText(extracted + ".idx") == "idx",
		"a log in a zip is extracted with its sidecar and index");

	var extractedOld = reader.Extract(oldLog.Location, companions, Path.Combine(root, "ExtractedOld"));
	Check(File.ReadAllText(extractedOld) == "old log\n" && File.Exists(extractedOld + ".xml") && !File.Exists(extractedOld + ".idx"),
		"a log in a zip in a zip is extracted with the companions it has");

	// Back and Forward.
	var history = new NavigationHistory();
	history.Visit(Location.ForFileSystem(logs));
	history.Visit(bundle);
	history.Visit(year.Location);
	Check(history.CanGoBack && !history.CanGoForward, "Back after going somewhere");
	Check(history.GoBack().Equals(bundle) && history.CanGoForward, "Back returns to the previous place");
	history.Visit(Location.ForFileSystem(root));
	Check(!history.CanGoForward, "going somewhere new clears Forward");
}
finally
{
	reader.Dispose();
	Directory.Delete(root, recursive: true);
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? $"All {checks} checks passed." : $"{failures} of {checks} checks failed.");
return failures == 0 ? 0 : 1;
