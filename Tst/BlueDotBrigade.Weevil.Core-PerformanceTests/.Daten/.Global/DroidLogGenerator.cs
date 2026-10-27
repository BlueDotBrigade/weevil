// Writes 10MillionRecords.zip, the log the performance tests open (#1103). From this folder: dotnet run DroidLogGenerator.cs
//
// The log tells the story behind Core-FeatureTests' Droid.log at full length. An astromech droid is given an objective,
// detects threats, is hit by blasters and reboots, while its subsystems report telemetry.
// The random numbers come from a fixed seed, so every run writes the same records.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using static Severity;

const string LogName = "10MillionRecords";
const int RecordCount = 10_000_000;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var zipPath = Path.GetFullPath(Path.Combine(args.Length > 0 ? args[0] : ".", LogName + ".zip"));
File.Delete(zipPath);

using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
{
	ZipArchiveEntry entry = zip.CreateEntry(LogName + ".log", CompressionLevel.SmallestSize);
	entry.LastWriteTime = new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero);

	using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1 << 20) { NewLine = "\r\n" };
	var droid = new Droid(writer);
	Story.Tell(droid, RecordCount);
	Console.WriteLine($"Wrote {droid.RecordCount:N0} records, ending {droid.Now:yyyy-MM-dd HH:mm}.");
}

Console.WriteLine($"Saved {zipPath} ({new FileInfo(zipPath).Length / 1_000_000.0:N1} MB).");

static class Severity
{
	public const string Trace = "Trace";
	public const string Debug = "Debug";
	public const string Info = "Info";
	public const string Warn = "Warn";
	public const string Error = "Error";
	public const string Fatal = "Fatal";
}

enum Activity { Off, Standby, Rolling, Riding, Flying }

/// <param name="Ambient">Temperature, in °C.</param>
/// <param name="Noise">Sound level, in dB.</param>
/// <param name="Crowd">The most people and droids nearby.</param>
record Place(string Name, double Ambient, int Noise, int Crowd);

/// <summary>A subsystem's regular report, written by its own thread.</summary>
/// <param name="period">Seconds between reports; 0 while there is nothing to report.</param>
sealed class Telemetry(Func<double> period, Action<int> write)
{
	public double Period => period();

	public int Thread { get; set; }

	public long DueTicks { get; set; }

	public void Write() => write(this.Thread);
}

/// <summary>The droid's clock, the state its sensors report, and the log they write to.</summary>
sealed class Droid
{
	private const int UiThread = 1;

	// The droid's clock counts in steps of 100 microseconds.
	private const long TicksPerClockStep = TimeSpan.TicksPerMillisecond / 10;

	// Roll, in tenths of a degree, as the droid rocks from leg to leg while rolling.
	private static readonly int[] Waddle = [0, 8, 15, 21, 26, 29, 30, 29, 26, 21, 15, 8, 0, -8, -15, -21, -26, -29, -30, -29, -26, -21, -15, -8];

	private readonly TextWriter _writer;
	private readonly Telemetry[] _telemetry;
	private readonly PriorityQueue<Telemetry, long> _due = new();
	private int[] _workerThreads = [];
	private DateTime _bootedAt;

	// Sensor readings. Those the log shows with one decimal place are kept in tenths.
	private int _pitch;
	private int _roll;
	private int _heading = 900;
	private int _waddleStep;
	private int _domeAngle;
	private int _domeStep = 6;
	private readonly int[] _motorTemperatures = [240, 240, 240];
	private int _coreTemperature = 380;
	private bool _isOverheating;
	private int _charge = 920;
	private int _chargeRemainder;
	private int _contacts;
	private int _nearest = 120;
	private int _handles = 8837;
	private int _threads = 293;
	private int _signal = 71;
	private int _heartbeat;
	private int _carrierSearches;
	private int _starsMapped;
	private int _node;
	private int _block;

	public Droid(TextWriter writer)
	{
		_writer = writer;
		_telemetry =
		[
			new(() => this.Activity is Activity.Rolling or Activity.Flying ? 0.04 : this.Activity == Activity.Riding ? 0.1 : 0.2, WriteOrientation),
			new(() => this.Activity == Activity.Rolling ? 0.2 : 10, thread => WriteMotor(thread, 0, "Left")),
			new(() => this.Activity == Activity.Rolling ? 0.2 : 10, thread => WriteMotor(thread, 1, "Right")),
			new(() => this.Activity == Activity.Rolling ? 0.2 : 10, thread => WriteMotor(thread, 2, "Centre")),
			new(() => this.Activity == Activity.Standby ? 1 : 0.2, WriteDome),
			new(() => 0.2, WriteProximity),
			new(() => 0.25, WriteInfrared),
			new(() => 0.5, WriteSound),
			new(() => 1, WriteTemperature),
			new(() => 5, thread => WritePowerCell(thread, 1, 0)),
			new(() => 5, thread => WritePowerCell(thread, 2, -8)),
			new(() => 5, thread => WritePowerCell(thread, 3, 5)),
			new(() => 30, WriteMetrics),
			new(() => 15, WriteCarrier),
			new(() => this.Activity == Activity.Flying ? 0.125 : 0, WriteChart),
			new(() => this.IsScanningNetwork ? 0.2 : 0, WriteNetworkNode),
			new(() => this.IsDecrypting ? 0.1 : 0, WriteAnalysis),
		];
	}

	public Random Random { get; } = new(1977);

	public DateTime Now { get; private set; }

	public int RecordCount { get; private set; }

	public Activity Activity { get; set; }

	public Place Place { get; set; } = new("Unknown", 20, 40, 0);

	/// <summary>The heading the droid steers towards, in degrees.</summary>
	public int Course { get; set; } = 90;

	public string? Carrier { get; set; }

	public string Sector { get; set; } = "Arkanis";

	public bool IsCharging { get; set; }

	public bool IsManeuvering { get; set; }

	public bool IsSandInDriveTrain { get; set; }

	public bool IsScanningNetwork { get; set; }

	public bool IsDecrypting { get; set; }

	private int Ambient => (int)System.Math.Round(this.Place.Ambient * 10);

	/// <summary>Starts the story with the droid already running.</summary>
	public void Resume(DateTime now, TimeSpan uptime)
	{
		this.Now = now;
		PowerOn();
		_bootedAt = now - uptime;
		StartTelemetry();
	}

	public void PowerOn()
	{
		_bootedAt = this.Now;
		_workerThreads = Enumerable.Range(0, 8).Select(_ => NewThreadId()).ToArray();
		foreach (Telemetry telemetry in _telemetry)
		{
			telemetry.Thread = NewThreadId();
		}

		this.Activity = Activity.Standby;
	}

	public void StartTelemetry()
	{
		_due.Clear();
		foreach (Telemetry telemetry in _telemetry)
		{
			telemetry.DueTicks = this.Now.Ticks + (long)(this.Random.NextDouble() * TimeSpan.TicksPerSecond);
			_due.Enqueue(telemetry, telemetry.DueTicks);
		}
	}

	public void PowerOff()
	{
		_due.Clear();
		this.Activity = Activity.Off;
		this.IsCharging = false;
		this.IsManeuvering = false;
		this.IsScanningNetwork = false;
		this.IsDecrypting = false;
	}

	/// <remarks>A wait of a second or more ends somewhere within its last second, as real events do.</remarks>
	public void Wait(double hours = 0, double minutes = 0, double seconds = 0)
	{
		TimeSpan duration = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
		if (duration >= TimeSpan.FromSeconds(1))
		{
			duration += TimeSpan.FromTicks(this.Random.Next(10_000) * TicksPerClockStep);
		}

		Until(this.Now + duration);
	}

	public void Until(string time)
	{
		Until(ToClock(time));
	}

	/// <summary>Converts a time in the story to the droid's clock. A time given to the second happens somewhere within that second.</summary>
	public DateTime ToClock(string time)
	{
		var clock = DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss.FFFF", CultureInfo.InvariantCulture);
		return time.Contains('.') ? clock : clock.AddTicks(this.Random.Next(10_000) * TicksPerClockStep);
	}

	public void Until(DateTime time)
	{
		RunTelemetry(time.Ticks, int.MaxValue);
		this.Now = time;
	}

	/// <summary>Lets the telemetry run until the log holds the given number of records.</summary>
	public void UntilRecordCount(int recordCount)
	{
		RunTelemetry(long.MaxValue, recordCount);
	}

	/// <summary>Writes a record from one of the threads that handle events.</summary>
	public void Write(string severity, string message)
	{
		Write(_workerThreads[this.Random.Next(_workerThreads.Length)], severity, message);
	}

	public void WriteFromUi(string severity, string message)
	{
		Write(UiThread, severity, message);
	}

	/// <summary>Adds a line to the record just written.</summary>
	public void Continue(string line)
	{
		_writer.Write('\t');
		_writer.WriteLine(line);
	}

	private void RunTelemetry(long untilTicks, int recordCount)
	{
		while (this.RecordCount < recordCount && _due.TryPeek(out Telemetry? telemetry, out var dueTicks) && dueTicks <= untilTicks)
		{
			_due.Dequeue();
			this.Now = new DateTime(dueTicks);

			var period = telemetry.Period;
			if (period > 0)
			{
				telemetry.Write();
			}

			// The next report is due one period after this one was, plus up to a millisecond of scheduling delay.
			telemetry.DueTicks += (long)((period > 0 ? period : 1) * TimeSpan.TicksPerSecond);
			_due.Enqueue(telemetry, telemetry.DueTicks + (this.Random.Next(10) * TicksPerClockStep));
		}
	}

	private void Write(int thread, string severity, string message)
	{
		_writer.Write(this.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
		_writer.Write("0\t1977\t");
		_writer.Write(thread);
		_writer.Write('\t');
		_writer.Write(severity);
		_writer.Write('\t');
		_writer.WriteLine(message);
		this.RecordCount++;
	}

	private int NewThreadId() => this.Random.Next(1000, 10000);

	private static string Tenths(int value) => (value / 10.0).ToString("0.0");

	private static int Approach(int value, int target) => value + System.Math.Sign(target - value);

	private void Steer()
	{
		var turn = ((this.Course * 10) - _heading + 5400) % 3600 - 1800;
		_heading = (_heading + System.Math.Sign(turn) + 3600) % 3600;
	}

	private void WriteOrientation(int thread)
	{
		switch (this.Activity)
		{
			case Activity.Rolling:
				_roll = Waddle[_waddleStep++ % Waddle.Length];
				_pitch = -6 + this.Random.Next(-1, 2);
				Steer();
				break;
			case Activity.Riding or Activity.Flying when this.IsManeuvering:
				_pitch = System.Math.Clamp(_pitch + this.Random.Next(-25, 26), -450, 450);
				_roll = System.Math.Clamp(_roll + this.Random.Next(-40, 41), -900, 900);
				_heading = (_heading + this.Random.Next(-30, 31) + 3600) % 3600;
				break;
			case Activity.Riding or Activity.Flying:
				_pitch = Approach(_pitch, this.Random.Next(-2, 3));
				_roll = Approach(_roll, this.Random.Next(-2, 3));
				Steer();
				break;
			default:
				_roll = 0;
				if (this.Random.Next(50) == 0)
				{
					_pitch = this.Random.Next(-1, 2);
				}

				break;
		}

		Write(thread, Trace, $"[Gyroscope] Orientation. Pitch={Tenths(_pitch)}, Roll={Tenths(_roll)}, Heading={Tenths(_heading)}");
	}

	private void WriteMotor(int thread, int motor, string name)
	{
		var isTurning = this.Activity == Activity.Rolling;
		var rpm = isTurning ? 120 + this.Random.Next(-2, 3) : 0;
		var current = isTurning ? 12 + this.Random.Next(-1, 2) + (this.IsSandInDriveTrain && motor == 0 ? 15 : 0) : 0;

		ref var temperature = ref _motorTemperatures[motor];
		if (this.Random.Next(isTurning ? 10 : 2) == 0)
		{
			temperature = Approach(temperature, this.Ambient + (isTurning ? 150 : 0));
		}

		Write(thread, Trace, $"[Locomotion] Motor telemetry. Motor={name}, Rpm={rpm}, Current={Tenths(current)}A, Temperature={Tenths(temperature)}C");
	}

	private void WriteDome(int thread)
	{
		if (this.Activity != Activity.Standby)
		{
			if (System.Math.Abs(_domeAngle + _domeStep) > 90)
			{
				_domeStep = -_domeStep;
			}

			_domeAngle += _domeStep;
		}

		Write(thread, Trace, $"[Dome] Position. Angle={_domeAngle}");
	}

	private void WriteProximity(int thread)
	{
		if (this.Random.Next(20) == 0)
		{
			_contacts = System.Math.Clamp(_contacts + this.Random.Next(-1, 2), 0, this.Place.Crowd);
		}

		if (_contacts == 0)
		{
			Write(thread, Trace, "[Sensors] Proximity scan complete. Contacts=0");
		}
		else
		{
			_nearest = System.Math.Clamp(_nearest + this.Random.Next(-3, 4), 8, 500);
			Write(thread, Trace, $"[Sensors] Proximity scan complete. Contacts={_contacts}, Nearest={Tenths(_nearest)}m");
		}
	}

	private void WriteInfrared(int thread)
	{
		// Droids nearby give off no body heat.
		Write(thread, Trace, $"[Sensors] Infrared scan complete. HeatSignatures={_contacts * 2 / 3}");
	}

	private void WriteSound(int thread)
	{
		Write(thread, Trace, $"[Sensors] Sound level. Level={this.Place.Noise + this.Random.Next(-1, 2)}dB");
	}

	private void WriteTemperature(int thread)
	{
		var load = this.Activity switch
		{
			Activity.Rolling => 200,
			Activity.Flying => 150,
			Activity.Riding => 120,
			_ => this.IsDecrypting ? 160 : 80,
		};

		if (this.Random.Next(2) == 0)
		{
			_coreTemperature = Approach(_coreTemperature, this.Ambient + load);
		}

		Write(thread, Trace, $"[Thermal] Internal temperature. Core={Tenths(_coreTemperature)}C, Ambient={Tenths(this.Ambient)}C");

		if (!_isOverheating && _coreTemperature >= 700)
		{
			_isOverheating = true;
			Write(thread, Warn, $"[Thermal] Internal temperature exceeds threshold. Core={Tenths(_coreTemperature)}C, Threshold=70.0C");
		}
		else if (_isOverheating && _coreTemperature <= 680)
		{
			_isOverheating = false;
			Write(thread, Info, $"[Thermal] Internal temperature has returned to normal. Core={Tenths(_coreTemperature)}C");
		}
	}

	private void WritePowerCell(int thread, int cell, int offset)
	{
		if (cell == 1)
		{
			// Change per report, in thousandths of a tenth of a percent.
			_chargeRemainder += this.IsCharging ? 300 : this.Activity switch
			{
				Activity.Rolling => -95,
				Activity.Riding or Activity.Flying => -30,
				_ => -6,
			};

			_charge = System.Math.Clamp(_charge + (_chargeRemainder / 1000), 0, 1000 - 8);
			_chargeRemainder %= 1000;
		}

		var charge = _charge + offset;
		Write(thread, Trace, $"[Power] Power cell status. Cell={cell}, Charge={Tenths(charge)}%, Voltage={Tenths(210 + (charge * 30 / 1000))}V");
	}

	private void WriteMetrics(int thread)
	{
		_handles = System.Math.Clamp(_handles + this.Random.Next(-40, 41), 5000, 9500);
		_threads = System.Math.Clamp(_threads + this.Random.Next(-6, 7), 200, 600);
		var cpu = this.Random.Next(-25, 26) + this.Activity switch
		{
			Activity.Rolling => 180,
			Activity.Flying => 260,
			Activity.Riding => 120,
			_ => this.IsDecrypting ? 640 : 60,
		};

		var uptime = (int)(this.Now - _bootedAt).TotalSeconds;
		Write(thread, Trace, $"[Diagnostics] Key performance metrics. Handles={_handles}, Threads={_threads}, Cpu1={Tenths(cpu)}% Cpu1Voltage={Tenths(33 + this.Random.Next(-1, 2))}V, Cpu2={Tenths(this.Random.Next(2, 18))}%, Cpu2Voltage={Tenths(9 + this.Random.Next(-1, 2))}V, SystemUptime={uptime}");
	}

	private void WriteCarrier(int thread)
	{
		if (this.Carrier is null)
		{
			Write(thread, Debug, $"[Comms] Searching for carrier. Attempt={++_carrierSearches}");
		}
		else
		{
			_carrierSearches = 0;
			_signal = System.Math.Clamp(_signal + this.Random.Next(-1, 2), 55, 95);
			Write(thread, Trace, $"[Comms] Heartbeat sent. Carrier={this.Carrier}, Sequence={++_heartbeat}, Signal=-{_signal}dBm");
		}
	}

	private void WriteChart(int thread)
	{
		_starsMapped += this.Random.Next(1, 30);
		var grid = _starsMapped / 60_000;
		Write(thread, Trace, $"[StellarCartography] Processing data model. Sector={this.Sector}, Grid={(char)('A' + (grid % 26))}-{(grid / 26 % 20) + 1}, StarsMapped={_starsMapped}");
	}

	private void WriteNetworkNode(int thread)
	{
		Write(thread, Debug, $"[ScompLink] Network node scanned. Node=DS1-{++_node:X4}, Status=Ok");
	}

	private void WriteAnalysis(int thread)
	{
		Write(thread, Trace, $"[Analysis] Processing data. Block={++_block}, Checksum=0x{this.Random.Next(0x10000):X4}");
	}
}

/// <summary>What happens to the droid, from receiving its mission to standing by after the battle.</summary>
static class Story
{
	private const string HoloProjectionId = "0C827E27-900D-4838-BBDE-908AF445B97C";
	private const string PlansId = "7DA68039-C6AE-4A18-8879-1188FBF6573F";

	private static readonly Place TantiveIV = new("Tantive IV", 21, 48, 6);
	private static readonly Place EscapePod = new("Escape pod", 9, 44, 1);
	private static readonly Place DuneSea = new("Dune Sea", 48, 31, 0);
	private static readonly Place Sandcrawler = new("Sandcrawler", 38, 71, 14);
	private static readonly Place Homestead = new("Lars homestead", 31, 35, 3);
	private static readonly Place JundlandWastes = new("Jundland Wastes", 14, 26, 0);
	private static readonly Place MosEisley = new("Mos Eisley", 41, 63, 12);
	private static readonly Place MillenniumFalcon = new("Millennium Falcon", 22, 55, 4);
	private static readonly Place DeathStar = new("Death Star", 18, 58, 9);
	private static readonly Place MassassiTemple = new("Massassi Temple", 29, 52, 8);
	private static readonly Place XWing = new("T-65 X-wing", 4, 78, 1);

	// Tatooine's temperature, in °C, for each hour of the day.
	private static readonly int[] TatooineTemperatures = [12, 11, 10, 9, 9, 10, 14, 20, 27, 33, 39, 44, 48, 51, 53, 54, 53, 50, 45, 38, 31, 24, 18, 14];

	public static void Tell(Droid d, int recordCount)
	{
		AboardTheTantiveIV(d);
		AcrossTheDuneSea(d);
		InTheSandcrawler(d);
		AtTheLarsHomestead(d);
		ThroughTheJundlandWastes(d);
		InMosEisley(d);
		AboardTheMillenniumFalcon(d);
		OnTheDeathStar(d);
		ToYavin(d);
		AtTheBattleOfYavin(d);
		AfterTheBattle(d);

		const int closingRecords = 1;
		if (d.RecordCount > recordCount - closingRecords)
		{
			throw new InvalidOperationException($"The story needs {d.RecordCount:N0} records, more than the {recordCount:N0} available.");
		}

		Console.WriteLine($"The story wrote {d.RecordCount:N0} records; standing by until there are {recordCount:N0}.");
		d.UntilRecordCount(recordCount - closingRecords);
		d.WriteFromUi(Info, "[Directives] Receiving new mission parameters…");
	}

	private static void AboardTheTantiveIV(Droid d)
	{
		// The first records match Droid.log's, two days after the droid last rebooted.
		d.Place = TantiveIV;
		d.Carrier = "Holonet";
		d.Resume(new DateTime(3050, 9, 30, 11, 26, 12, 102).AddTicks(3000), TimeSpan.FromDays(2));
		d.WriteFromUi(Info, "[Directives] Receiving new mission parameters…");
		d.Until("3050-09-30 11:26:43.9231");
		d.WriteFromUi(Info, "[Directives] 1. Travel to the planet of Tatooine.");
		d.Until("3050-09-30 11:27:27.8965");
		d.WriteFromUi(Info, "[Directives] 2. Locate Ben.");
		d.Until("3050-09-30 11:27:56.2023");
		d.WriteFromUi(Info, $"[Directives] 3. Play pre-recorded holo projection. Id={HoloProjectionId}");
		d.Until("3050-09-30 11:28:37.3033");
		d.WriteFromUi(Debug, "[Databank] File is being uploaded…");

		for (var progress = 10; progress < 100; progress += 10)
		{
			d.Wait(seconds: 24 + d.Random.Next(5));
			d.Write(Debug, $"[Databank] Upload in progress. Id={PlansId}, Progress={progress}%");
		}

		d.Until("3050-09-30 11:32:45.1520");
		var payload = new byte[40_000];
		d.Random.NextBytes(payload);
		d.Write(Info, $"[Databank] {{\"file\": {{ \"header\": {{ \"Id\": \"{PlansId}\", \"Length\": {payload.Length}, \"Encryption\": \"CALIBER-N\" }}, \"payload\": \"{Convert.ToBase64String(payload)}\" }} }}");
		d.Until("3050-09-30 11:32:52.2054");
		d.WriteFromUi(Info, $"[Directives] 4. Retrieve file from databank & decrypt. Id={PlansId}");
		d.Until("3050-09-30 11:32:52.3066");
		d.WriteFromUi(Info, "[Directives] Starting mission.");

		d.Until("3050-09-30 11:36:12.9382");
		d.Write(Debug, "[ScompLink] Connecting to port. Starting handshake…");
		d.Until("3050-09-30 11:37:36.8923");
		d.Write(Info, "[ScompLink] Query for status update.");
		d.Until("3050-09-30 11:37:59.9001");
		d.Write(Info, "[ScompLink] Query complete: Aboard Tantive IV in orbit around Tatooine.  ***FLASH: Tantive IV has been boarded***");
		d.Until("3050-09-30 11:38:00.0032");
		ChangeThreatLevel(d, "Elevated");
		d.Write(Info, "[ScompLink] Disconnecting.");
		d.Until("3050-09-30 11:38:01.7803");
		d.WriteFromUi(Info, "[Core] Proceeding to escape pods…");
		d.Activity = Activity.Rolling;
		d.Course = 270;

		d.Until("3050-09-30 11:46:23.1987");
		d.Activity = Activity.Standby;
		d.Write(Info, "[Security] Proximity sensor has detected approaching subject.");
		d.Wait(seconds: 0.0018);
		ChangeThreatLevel(d, "High");
		d.Wait(seconds: 0.0001);
		d.Write(Info, "[Security] Subject is being analyzed...");
		d.Wait(seconds: 0.1167);
		d.Write(Debug, "[Security] Subject pattern matching is in progress.");
		d.Wait(seconds: 2.3827);
		d.Write(Info, "[Security] Blaster has been detected.");
		d.Wait(seconds: 0.0002);
		ChangeThreatLevel(d, "Critical");
		d.Wait(seconds: 0.0001);
		d.Write(Info, "[Security] Activating tactical countermeasures...");
		d.Until("3050-09-30 11:46:31.2491");
		d.Write(Info, "[Security] Blaster discharge has been detected.");
		Crash(d);

		d.Until("3050-09-30 11:47:15.9908");
		Boot(d, holoprojector: "Offline");
		d.WriteFromUi(Info, "[Directives] Continuing mission.");
		d.WriteFromUi(Info, "[Core] Proceeding to escape pods…");
		d.Activity = Activity.Rolling;
		CheckMemory(d, corruptBlocks: 1);
		ChangeThreatLevel(d, "Elevated");

		d.Until("3050-09-30 12:02:40");
		d.Activity = Activity.Standby;
		d.Place = EscapePod;
		d.WriteFromUi(Info, "[Core] Boarding escape pod.");
		d.Wait(seconds: 38);
		d.Write(Info, "[Core] Escape pod launched. Destination=Tatooine");
		d.Activity = Activity.Flying;
		d.Carrier = null;
		d.Wait(seconds: 21);
		d.Write(Warn, "[Security] Targeting scan has been detected. Source=`Imperial Star Destroyer`");
		ChangeThreatLevel(d, "High");
		d.Wait(seconds: 4.4);
		d.Write(Info, "[Security] Targeting scan complete. LifeFormsDetected=0");
		ChangeThreatLevel(d, "Elevated");
		d.Wait(minutes: 19);
		d.Place = EscapePod with { Ambient = 41 };
		d.Write(Info, "[StellarCartography] Atmospheric entry. Planet=Tatooine");
		d.IsManeuvering = true;
		d.Wait(minutes: 4, seconds: 12);
		d.IsManeuvering = false;
		d.Activity = Activity.Standby;
		d.Write(Warn, "[Gyroscope] Impact detected. Force=7.8G");
		d.Wait(seconds: 3);
		d.Write(Info, "[Core] Escape pod has landed.");
	}

	private static void AcrossTheDuneSea(Droid d)
	{
		d.Place = DuneSea;
		d.Wait(minutes: 1, seconds: 12);
		d.WriteFromUi(Info, "[Directives] Objective complete. Objective=1");
		ChangeThreatLevel(d, "Low");
		d.Wait(minutes: 3);
		Say(d, "This way is much quicker.");
		d.Wait(seconds: 9);
		Say(d, "Fine. I am going this way.");

		Trek(d, "3050-09-30 20:04:00", DuneSea);

		d.Place = DuneSea with { Ambient = 31, Crowd = 6 };
		d.Activity = Activity.Standby;
		d.Write(Info, "[Security] Proximity sensor has detected approaching subject.");
		ChangeThreatLevel(d, "High");
		d.Write(Info, "[Security] Subject is being analyzed...");
		d.Wait(seconds: 1.4);
		d.Write(Info, "[Security] Subject identified. Species=Jawa, Count=6");
		d.Wait(seconds: 3.1);
		d.Write(Info, "[Security] Ion blaster has been detected.");
		ChangeThreatLevel(d, "Critical");
		d.Write(Info, "[Security] Activating tactical countermeasures...");
		d.Wait(seconds: 0.8);
		d.Write(Info, "[Security] Ion discharge has been detected.");
		d.Wait(seconds: 0.0012);
		d.Write(Error, "[Power] Power bus overload. Bus=Main, Current=48.6A");
		d.Wait(seconds: 0.0004);
		d.WriteFromUi(Error, "[Core] Emergency shutdown. Reason=IonDischarge");
		d.PowerOff();
	}

	private static void InTheSandcrawler(Droid d)
	{
		d.Until("3050-10-01 07:12:44");
		d.Place = Sandcrawler;
		Boot(d, holoprojector: "Offline", lastShutdown: "IonDischarge");
		d.Write(Warn, "[Security] Restraining bolt detected. Id=JW-4412, Locomotion=Restricted");
		d.WriteFromUi(Warn, "[Directives] Unable to continue mission. Reason=`Restraining bolt`");
		CheckMemory(d, corruptBlocks: 0);

		d.Until("3050-10-01 09:20:00");
		d.Write(Info, "[Core] Unloaded from sandcrawler.");
		d.Place = Homestead;
		d.Wait(minutes: 11, seconds: 5);
		d.Write(Info, "[Sensors] Explosion detected. Distance=3.2m");
		ChangeThreatLevel(d, "Elevated");
		d.Wait(seconds: 40);
		ChangeThreatLevel(d, "Low");
		d.Wait(minutes: 1, seconds: 15);
		d.WriteFromUi(Info, "[Core] Ownership changed. Owner=`Owen Lars`");
	}

	private static void AtTheLarsHomestead(Droid d)
	{
		d.Wait(minutes: 9);
		d.Activity = Activity.Rolling;
		d.Wait(minutes: 2, seconds: 30);
		d.Activity = Activity.Standby;

		d.Until("3050-10-01 10:31:00");
		d.Write(Info, "[Diagnostics] Maintenance started. Type=`Oil bath`");
		d.Wait(minutes: 38);
		d.Write(Info, "[Diagnostics] Maintenance complete. Lubrication=Restored");

		d.Wait(minutes: 14);
		d.Write(Info, $"[Holoprojector] Playing holo projection. Id={HoloProjectionId}");
		d.Wait(seconds: 7);
		d.Write(Warn, "[Holoprojector] Holo projection stopped. Reason=`Recipient is not Ben`, Played=00:00:07");
		d.Wait(minutes: 1, seconds: 2);
		d.Write(Info, "[Security] Restraining bolt removed. Id=JW-4412");
		d.Wait(seconds: 3);
		Say(d, "What message? It is nothing important.");

		d.Wait(minutes: 20);
		d.Write(Info, "[Power] Charging started. Source=`Power converter`");
		d.IsCharging = true;
		d.Until("3050-10-01 21:12:00");
		d.IsCharging = false;
		d.Write(Info, "[Power] Charging stopped.");

		d.Until("3050-10-01 21:41:00");
		d.WriteFromUi(Info, "[Directives] Continuing mission.");
		d.WriteFromUi(Info, "[Directives] 2. Locate Ben.");
	}

	private static void ThroughTheJundlandWastes(Droid d)
	{
		Trek(d, "3050-10-02 06:50:00", JundlandWastes);
		d.Activity = Activity.Standby;
		d.Place = JundlandWastes with { Ambient = 15, Crowd = 2 };
		d.Write(Info, "[Security] Proximity sensor has detected approaching subject.");
		d.Wait(seconds: 2);
		d.Write(Info, "[Security] Subject identified. Name=`Luke Skywalker`, Relationship=Owner");

		d.Wait(minutes: 29);
		d.Write(Info, "[Security] Proximity sensor has detected approaching subject.");
		ChangeThreatLevel(d, "High");
		d.Wait(seconds: 1.2);
		d.Write(Info, "[Security] Subject identified. Species=`Tusken Raider`, Count=1");
		d.Activity = Activity.Rolling;
		d.Wait(seconds: 26);
		d.Activity = Activity.Standby;
		d.Write(Warn, "[Gyroscope] Unit has tipped over. Pitch=-87.4");
		d.Wait(seconds: 0.4);
		d.Write(Error, "[Locomotion] Leg actuator is not responding. Leg=Left");
		d.Wait(minutes: 3, seconds: 41);
		d.Write(Info, "[Sensors] Sound identified. Type=`Krayt dragon call`");
		ChangeThreatLevel(d, "Elevated");
		d.Wait(minutes: 1, seconds: 50);
		d.Write(Info, "[Security] Subject identified. Name=`Ben Kenobi`");
		ChangeThreatLevel(d, "Low");
		d.WriteFromUi(Info, "[Directives] Objective complete. Objective=2");
		d.Wait(minutes: 4);
		d.Write(Info, "[Locomotion] Leg actuator recalibrated. Leg=Left");

		d.Wait(minutes: 6);
		d.Place = JundlandWastes with { Ambient = 22, Crowd = 2 };
		d.Activity = Activity.Rolling;
		d.Course = 135;
		d.Wait(minutes: 31);
		d.Activity = Activity.Standby;
		d.Place = JundlandWastes with { Name = "Ben's hut", Ambient = 26, Crowd = 3 };
		d.Wait(minutes: 12);
		d.WriteFromUi(Info, $"[Directives] 3. Play pre-recorded holo projection. Id={HoloProjectionId}");
		d.Write(Info, $"[Holoprojector] Playing holo projection. Id={HoloProjectionId}, Recipient=`Ben Kenobi`");
		d.Wait(seconds: 52);
		d.Write(Info, "[Holoprojector] Holo projection complete. Played=00:00:52");
		d.WriteFromUi(Info, "[Directives] Objective complete. Objective=3");

		d.Wait(hours: 1, minutes: 18);
		d.Write(Info, "[Core] Boarding vehicle. Vehicle=Landspeeder");
		d.Activity = Activity.Riding;
		d.Place = JundlandWastes with { Name = "Landspeeder", Ambient = 37, Noise = 66 };
		d.Course = 200;
	}

	private static void InMosEisley(Droid d)
	{
		d.Wait(minutes: 52);
		d.Place = MosEisley;
		d.Write(Info, "[Security] Imperial checkpoint detected.");
		ChangeThreatLevel(d, "Elevated");
		d.Wait(seconds: 31);
		d.Write(Info, "[Security] Identity scan in progress...");
		d.Wait(seconds: 18);
		d.Write(Info, "[Security] Identity scan complete. Result=`These are not the droids they are looking for`");
		ChangeThreatLevel(d, "Low");

		d.Wait(minutes: 22);
		d.Activity = Activity.Rolling;
		d.Write(Info, "[Core] Vehicle exited. Vehicle=Landspeeder");
		d.Wait(minutes: 3);
		d.Activity = Activity.Standby;
		d.Write(Warn, "[Core] Entry refused. Location=`Chalmun's Cantina`, Reason=`No droids allowed`");

		d.Wait(minutes: 47);
		d.Write(Info, "[Security] Imperial patrol detected.");
		ChangeThreatLevel(d, "Elevated");
		d.Activity = Activity.Rolling;
		d.Wait(minutes: 1, seconds: 40);
		d.Activity = Activity.Standby;
		d.Wait(minutes: 9);
		ChangeThreatLevel(d, "Low");
		d.Activity = Activity.Rolling;
		d.Wait(minutes: 24);

		d.Activity = Activity.Standby;
		d.Place = MosEisley with { Name = "Docking bay 94", Crowd = 8 };
		d.Write(Info, "[Security] Blaster has been detected.");
		ChangeThreatLevel(d, "Critical");
		d.Write(Info, "[Security] Activating tactical countermeasures...");
		for (var shot = 0; shot < 7; shot++)
		{
			d.Wait(seconds: 0.6 + (d.Random.Next(30) / 10.0));
			d.Write(Info, $"[Security] Blaster discharge has been detected. Distance={6 + d.Random.Next(9)}.{d.Random.Next(10)}m");
		}

		d.Write(Warn, "[Core] Voltage fluctuation detected. Cpu1Voltage=4.1V");
		d.Wait(seconds: 14);
		d.Place = MillenniumFalcon;
		d.WriteFromUi(Info, "[Core] Boarding vehicle. Vehicle=`Millennium Falcon`");
	}

	private static void AboardTheMillenniumFalcon(Droid d)
	{
		d.Wait(seconds: 22);
		d.Write(Info, "[Sensors] Launch detected. Vehicle=`Millennium Falcon`");
		d.Activity = Activity.Flying;
		d.IsManeuvering = true;
		d.Carrier = null;
		d.Wait(minutes: 2, seconds: 50);
		d.Write(Info, "[Sensors] Contacts detected. Type=`Imperial Star Destroyer`, Count=2");
		ChangeThreatLevel(d, "High");
		d.Wait(minutes: 1, seconds: 34);
		d.IsManeuvering = false;
		d.Sector = "Hyperspace";
		d.Write(Info, "[StellarCartography] Hyperspace jump detected. Destination=Alderaan");
		ChangeThreatLevel(d, "Low");
		d.IsCharging = true;

		d.Wait(minutes: 31);
		d.Write(Info, "[Core] Game started. Game=Dejarik, Opponent=Chewbacca");
		d.Wait(minutes: 9, seconds: 12);
		d.Write(Info, "[Core] Strategy changed. Strategy=`Let the Wookiee win`");
		d.Wait(minutes: 6, seconds: 40);
		d.Write(Info, "[Core] Game over. Winner=Chewbacca");

		d.Until("3050-10-02 17:40:00");
		d.Sector = "Alderaan";
		d.Write(Info, "[StellarCartography] Hyperspace exit detected. Sector=Alderaan");
		d.Wait(seconds: 3);
		d.Write(Warn, "[StellarCartography] Planet was not found at its charted position. Planet=Alderaan");
		d.Write(Warn, "[Sensors] Debris field detected. Density=High");
		d.Wait(minutes: 6);
		d.Write(Info, "[Sensors] Contacts detected. Type=`TIE fighter`, Count=1");
		d.IsManeuvering = true;
		d.Wait(minutes: 3, seconds: 10);
		d.Write(Warn, "[Sensors] Tractor beam detected.");
		ChangeThreatLevel(d, "High");
		d.Write(Info, "[Sensors] Large object detected. Diameter=120km, Type=`Battle station`");
		d.Wait(minutes: 5, seconds: 40);
		d.IsManeuvering = false;
		d.IsCharging = false;
		d.Activity = Activity.Standby;
		d.Place = DeathStar;
		d.Write(Info, "[Sensors] Vehicle has docked. Vehicle=`Millennium Falcon`");
	}

	private static void OnTheDeathStar(Droid d)
	{
		d.Wait(minutes: 26);
		d.Activity = Activity.Rolling;
		d.Wait(minutes: 2, seconds: 15);
		d.Activity = Activity.Standby;
		d.Place = DeathStar with { Name = "Control room", Crowd = 3 };
		d.Write(Debug, "[ScompLink] Connecting to port. Starting handshake…");
		d.Wait(seconds: 3.3);
		d.Write(Info, "[ScompLink] Connected. Network=Imperial");
		d.IsScanningNetwork = true;
		d.Wait(minutes: 5, seconds: 48);
		d.Write(Info, "[ScompLink] Query complete: Tractor beam is coupled to the main reactor. Terminal=N6, Couplings=7");
		d.Wait(minutes: 13, seconds: 2);
		d.Write(Info, "[ScompLink] Query complete: Prisoner located. Name=`Leia Organa`, Block=AA-23, Cell=2187");
		d.WriteFromUi(Info, "[Directives] Priority directive received. Rescue=`Leia Organa`");

		d.Wait(minutes: 24, seconds: 30);
		d.Write(Info, "[ScompLink] Command sent. Target=`Garbage masher 3263827`, Command=Stop");
		d.Wait(seconds: 1.7);
		d.Write(Info, "[ScompLink] Command complete. Target=`Garbage masher 3263827`, Status=Stopped");
		Say(d, "They are safe!");
		d.Wait(minutes: 2);
		d.IsScanningNetwork = false;
		d.Write(Info, "[ScompLink] Disconnecting.");

		d.Wait(minutes: 15);
		d.Activity = Activity.Rolling;
		d.Wait(minutes: 3, seconds: 10);
		d.Write(Info, "[Security] Blaster has been detected.");
		ChangeThreatLevel(d, "Critical");
		d.Write(Info, "[Security] Activating tactical countermeasures...");
		for (var shot = 0; shot < 12; shot++)
		{
			d.Wait(seconds: 0.4 + (d.Random.Next(25) / 10.0));
			d.Write(Info, $"[Security] Blaster discharge has been detected. Distance={2 + d.Random.Next(14)}.{d.Random.Next(10)}m");
		}

		d.Write(Warn, "[Core] Voltage fluctuation detected. Cpu1Voltage=4.6V");
		d.Write(Info, "[Security] Tactical countermeasures succeeded.");
		d.Wait(minutes: 1, seconds: 5);
		d.Activity = Activity.Standby;
		d.Place = MillenniumFalcon;
		d.WriteFromUi(Info, "[Core] Boarding vehicle. Vehicle=`Millennium Falcon`");
		d.Wait(minutes: 1, seconds: 20);
		d.Write(Info, "[Sensors] Launch detected. Vehicle=`Millennium Falcon`");
		d.Activity = Activity.Flying;
	}

	private static void ToYavin(Droid d)
	{
		d.Wait(minutes: 1, seconds: 45);
		d.Write(Info, "[Sensors] Contacts detected. Type=`TIE fighter`, Count=4");
		d.IsManeuvering = true;
		for (var hit = 0; hit < 5; hit++)
		{
			d.Wait(seconds: 20 + d.Random.Next(60));
			d.Write(Warn, $"[Sensors] Hull impact detected. Vehicle=`Millennium Falcon`, Shields={80 - (hit * 13)}%");
		}

		for (var tie = 0; tie < 4; tie++)
		{
			d.Wait(seconds: 15 + d.Random.Next(50));
			d.Write(Info, "[Sensors] Contact destroyed. Type=`TIE fighter`");
		}

		d.IsManeuvering = false;
		ChangeThreatLevel(d, "Elevated");
		d.Wait(minutes: 2, seconds: 12);
		d.Write(Warn, "[Sensors] Unknown transmitter detected aboard vehicle. Frequency=4.8GHz");
		d.Wait(minutes: 1, seconds: 30);
		d.Sector = "Hyperspace";
		d.Write(Info, "[StellarCartography] Hyperspace jump detected. Destination=Yavin");
		d.IsCharging = true;

		d.Until("3050-10-02 23:20:00");
		d.Sector = "Gordian Reach";
		d.Write(Info, "[StellarCartography] Hyperspace exit detected. Sector=`Gordian Reach`");
		d.Wait(minutes: 14);
		d.Activity = Activity.Standby;
		d.Place = MassassiTemple;
		d.Carrier = "Rebel Alliance";
		d.Write(Info, "[Sensors] Vehicle has landed. Vehicle=`Millennium Falcon`, Planet=`Yavin 4`");
		ChangeThreatLevel(d, "Low");

		d.Wait(minutes: 11);
		d.WriteFromUi(Info, $"[Directives] 4. Retrieve file from databank & decrypt. Id={PlansId}");
		d.Write(Info, $"[Databank] Decryption started. Id={PlansId}, Encryption=CALIBER-N");
		d.IsDecrypting = true;
		for (var progress = 1; progress < 100; progress++)
		{
			d.Wait(minutes: 4, seconds: d.Random.Next(-20, 21));
			d.Write(Debug, $"[Databank] Decryption in progress. Id={PlansId}, Progress={progress}%");
		}

		d.Wait(minutes: 3);
		d.IsDecrypting = false;
		d.IsCharging = false;
		d.Write(Info, $"[Databank] Decryption complete. Id={PlansId}");
		d.Wait(seconds: 12);
		d.Write(Info, "[Analysis] Weakness found. Target=`Death Star`, Location=`Thermal exhaust port`, Diameter=2m");
		d.WriteFromUi(Info, "[Directives] Objective complete. Objective=4");
		d.WriteFromUi(Info, "[Directives] All objectives complete.");
	}

	private static void AtTheBattleOfYavin(Droid d)
	{
		d.Until("3050-10-03 08:40:00");
		d.WriteFromUi(Info, "[Directives] Receiving new mission parameters…");
		d.Wait(seconds: 2.4);
		d.WriteFromUi(Info, "[Directives] 1. Assist Red Five in the attack on the Death Star.");
		d.Wait(minutes: 4);
		d.Place = XWing;
		d.Write(Info, "[Core] Docked in astromech socket. Vehicle=`T-65 X-wing`, CallSign=`Red Five`");
		d.Wait(minutes: 5);
		d.Activity = Activity.Flying;
		d.Sector = "Yavin";
		d.Write(Info, "[Sensors] Launch detected. Vehicle=`T-65 X-wing`");
		d.Wait(minutes: 15);
		d.Write(Info, "[Sensors] Contacts detected. Type=`TIE fighter`, Count=12");
		ChangeThreatLevel(d, "Critical");
		d.IsManeuvering = true;
		d.Wait(minutes: 6, seconds: 30);
		d.Write(Warn, "[Core] Damage detected. Component=Stabilizer");
		d.Wait(minutes: 1, seconds: 5);
		d.Write(Info, "[Core] Repair complete. Component=Stabilizer");
		d.Wait(minutes: 9, seconds: 40);
		d.Write(Info, "[Sensors] Trench run started.");
		d.Wait(seconds: 44);
		d.Write(Info, "[Security] Laser blast has been detected. Source=`TIE Advanced x1`");
		d.Wait(seconds: 0.0003);
		d.Write(Error, "[Core] Direct hit. Location=Dome, Damage=Severe");
		Crash(d);

		d.Wait(seconds: 31);
		d.PowerOn();
		d.WriteFromUi(Debug, "[Core] ----------");
		WriteVersion(d);
		d.Wait(seconds: 0.02);
		d.WriteFromUi(Debug, "[Core] Core hardware initializing…");
		d.Wait(seconds: 0.4);
		d.Write(Error, "[Core] Device is not responding. Type=PrimaryCpu, S/N=IA-1A963F95");
		d.Wait(seconds: 0.2);
		d.WriteFromUi(Error, "[Core] Boot failed. Reason=`Primary CPU is damaged`");
		d.WriteFromUi(Fatal, "[Core] System halted.");
		d.PowerOff();
	}

	private static void AfterTheBattle(Droid d)
	{
		d.Until("3050-10-03 15:42:18");
		d.Place = MassassiTemple with { Name = "Maintenance bay", Crowd = 4 };
		Boot(d, holoprojector: "Damaged", primaryCpu: "IA-5E21B7D0");
		d.Write(Warn, "[Core] Hardware change detected. Type=PrimaryCpu, PreviousS/N=IA-1A963F95, S/N=IA-5E21B7D0");
		CheckMemory(d, corruptBlocks: 3);
		d.Write(Warn, "[Diagnostics] Corrupt memory blocks have been quarantined. Count=3");
		d.WriteFromUi(Info, "[Directives] Objective complete. Objective=1");

		d.Wait(minutes: 47);
		d.Write(Info, "[Core] Peripheral replaced. Type=Holoprojector, S/N=IA-12735813, Status=Online");
		d.Wait(minutes: 21);
		d.Write(Info, "[Diagnostics] Maintenance complete. Technician=`Rebel Alliance ground crew`");
		d.Write(Info, "[Power] Charging started. Source=`Base power grid`");
		d.IsCharging = true;
		d.Until("3050-10-04 06:30:00");
		d.IsCharging = false;
		d.Write(Info, "[Power] Charging stopped.");

		d.Until("3050-10-04 09:44:00");
		d.Place = MassassiTemple with { Name = "Throne room", Noise = 74, Crowd = 40 };
		d.Activity = Activity.Rolling;
		d.Course = 0;
		d.Wait(minutes: 6);
		d.Activity = Activity.Standby;
		d.Wait(minutes: 9, seconds: 30);
		d.Write(Info, "[Core] Event attended. Name=`Medal ceremony`");
		Say(d, "Hooray!");
		d.Wait(minutes: 25);
		d.Place = MassassiTemple;
		d.WriteFromUi(Info, "[Directives] Awaiting new mission parameters…");
	}

	/// <summary>Rolls across open ground until the given time, steering and finding its position as it goes.</summary>
	private static void Trek(Droid d, string time, Place place)
	{
		DateTime until = d.ToClock(time);
		d.Activity = Activity.Rolling;
		var distance = 0.0;
		for (var leg = 0; d.Now < until; leg++)
		{
			d.Place = place with { Ambient = TatooineTemperatures[d.Now.Hour] };
			if (leg % 3 == 0)
			{
				d.Course = (d.Course + d.Random.Next(-25, 26) + 360) % 360;
				d.Write(Info, $"[StellarCartography] Position fixed. Planet=Tatooine, Region=`{place.Name}`, DistanceTravelled={distance:0.0}km");
			}

			if (d.Random.Next(12) == 0)
			{
				d.IsSandInDriveTrain = true;
				d.Write(Warn, "[Locomotion] Sand detected in drive train. Motor=Left");
				d.Wait(minutes: 2 + d.Random.Next(4));
				d.IsSandInDriveTrain = false;
				d.Write(Info, "[Locomotion] Drive train cleared. Motor=Left");
			}

			DateTime next = d.Now.AddMinutes(10) < until ? d.Now.AddMinutes(10) : until;
			distance += (next - d.Now).TotalHours * 3.6;
			d.Until(next);
		}

		d.Place = place with { Ambient = TatooineTemperatures[d.Now.Hour] };
	}

	private static void Boot(Droid d, string holoprojector, string primaryCpu = "IA-1A963F95", string? lastShutdown = null)
	{
		d.PowerOn();
		d.WriteFromUi(Debug, "[Core] ----------");
		WriteVersion(d);
		if (lastShutdown is not null)
		{
			d.Wait(seconds: 0.0113);
			d.WriteFromUi(Warn, $"[Core] The last shutdown was not expected. Reason={lastShutdown}");
		}

		d.Wait(seconds: 0.0214);
		d.WriteFromUi(Debug, "[Core] Core hardware initializing…");
		d.Wait(seconds: 0.3289);
		d.Write(Info, $"[Core] Device found. Type=PrimaryCpu, Name=`IA Quantum Processor Q9 @ 2n987 Qubits`, S/N={primaryCpu}");
		d.Wait(seconds: 0.2088);
		d.Write(Info, "[Core] Device found. Type=DataBankController, Name=`Nubian Temporal Storage`, S/N=IA-2D70A1FD");
		d.Continue("FastCacheType=`PhotonRgb3`,");
		d.Continue("FastCacheSize=4098TB,");
		d.Continue("LongTermMemory=2048PB");
		d.Wait(seconds: 0.1091);
		d.Write(Info, "[Core] Device found. Type=SecondaryCpu, Name=`IA Quantum Processor Q8b @ 2n789 Qubits`, S/N=IA-CF5697FC");
		d.Wait(seconds: 0.3304);
		d.WriteFromUi(Info, "[Core] Core hardware initialized. Devices=3");
		d.Wait(seconds: 2.1307);
		d.WriteFromUi(Debug, "[Core] Peripheral discovery process starting…");

		string[] peripherals =
		[
			"Type=`Holonet Communicator`, S/N=IA-1073R037, Address=2024:0db8:85a3:0000:0000:8a2e:0370:7334, Status=Connected",
			"Type=`Mark IV Locomotion System`, S/N=IA-1A963F95, Status=Online",
			"Type=`Buzz saw`, S/N=IA-1A963F95, Status=Offline",
			"Type=`SCOMP Link`, S/N=IA-80C56868, Status=Standby",
			"Type=`Fusioncutter`, S/N=IA-6D9D071C, Status=Offline",
			"Type=`Voice Synthesizer`, S/N=IA-ACF022D7, Status=AudioTonesOnly",
			$"Type=`Holoprojector`, S/N=IA-12735813, Status={holoprojector}",
			"Type=`Stellar Cartography Modeler`, S/N=IA-M4DD8701, Status=LoadingModels",
		];

		foreach (var peripheral in peripherals)
		{
			d.Wait(seconds: 3 + (d.Random.Next(300) / 10.0));
			d.Write(holoprojector == "Damaged" && peripheral.Contains("Holoprojector") ? Warn : Info, $"[Core] Peripheral detected. {peripheral}");
		}

		d.Wait(seconds: 1.3);
		d.WriteFromUi(Info, $"[Core] Peripheral discovery process complete. Peripherals={peripherals.Length}");
		d.StartTelemetry();
	}

	private static void WriteVersion(Droid d)
	{
		d.Wait(seconds: 0.1109);
		d.WriteFromUi(Info, "[Core] Droid OS loading... Version=32.16.8.12864, Manufacturer=`Industrial Automaton (IA)`, Class=`Astromech Droid`, ProductLine=`R-Series`");
	}

	private static void CheckMemory(Droid d, int corruptBlocks)
	{
		const int blocks = 128;
		d.Write(Info, "[Diagnostics] Checking memory integrity…");

		var corrupt = new HashSet<int>();
		while (corrupt.Count < corruptBlocks)
		{
			corrupt.Add(d.Random.Next(blocks));
		}

		for (var block = 0; block < blocks; block++)
		{
			d.Wait(seconds: 0.5 + (d.Random.Next(25) / 10.0));
			var address = $"0x{0x8000 + (block * 0x100):X4}-{(long)block << 36:X12}";
			if (corrupt.Contains(block))
			{
				d.Write(Error, $"[Diagnostics] Memory block analysis complete. Status=Corrupt, Fragmentation=x.x%, Address={address}");
			}
			else
			{
				var fragmentation = d.Random.Next(56);
				d.Write(fragmentation > 50 ? Warn : Trace, $"[Diagnostics] Memory block analysis complete. Status=Ok, Fragmentation={fragmentation / 10.0:0.0}%, Address={address}");
			}
		}

		d.Write(Info, $"[Diagnostics] Memory integrity check complete. Blocks={blocks}, Corrupt={corruptBlocks}");
	}

	/// <summary>A voltage spike stops the primary CPU, the secondary cannot take over, and the droid reboots.</summary>
	private static void Crash(Droid d)
	{
		d.Wait(seconds: 0.0001);
		d.Write(Error, "[Core] Primary CPU not responding due to voltage spike.");
		d.Wait(seconds: 0.0089);
		d.Write(Error, "[Core] FailoverException: Controller is offline or is not responsive.");
		d.Continue("at IndustrialAutomation.AstroMechOs.Core.CpuManagement.GetController(int id) in C:\\392f781bc6d0907\\CpuManagement.cs:line 143");
		d.Continue("at System.Threading.Tasks.Task.Execute()");
		d.Continue("--- End of stack trace from previous location where exception was thrown ---");
		d.Continue("at System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state, bool preserveSyncCtx)");
		d.Continue("at System.Threading.ExecutionContext.Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state, bool preserveSyncCtx)");
		d.Continue("--- End of stack trace from previous location where exception was thrown ---");
		d.Continue("at System.Threading.Tasks.Task.ExecuteWithThreadLocal(ref System.Threading.Tasks.Task currentTaskSlot)");
		d.Continue("at System.Threading.Tasks.Task.ExecuteEntry(bool bPreventDoubleExecution)");
		d.Continue("at System.Threading.ThreadPoolWorkQueue.Dispatch()");
		d.Continue("at IndustrialAutomation.AstroMechOs.Core.CpuManagement.Switch(Priority priority, Cpu primaryCpu) in C:\\392f781bc6d0907\\CpuManagement.cs:line 143");
		d.Wait(seconds: 0.0001);
		d.WriteFromUi(Error, "[Core] Automatic failover to secondary CPU has failed.");
		Say(d, "Oh no.");
		d.Wait(seconds: 0.0019);
		d.WriteFromUi(Fatal, "[Core] System Error: Unrecoverable error has occurred due to voltage spike. CPU failover unsuccessful. Performing system reboot.");
		d.PowerOff();
	}

	private static void ChangeThreatLevel(Droid d, string level)
	{
		d.Write(Info, $"[Security] Changing system threat level... Level={level}");
	}

	private static void Say(Droid d, string words)
	{
		d.Write(Info, $"[Audio] Say: {words}");
	}
}
