namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// The sections and regions a log's sidecar lists, which the Open As dialog offers (#1214).
	/// </summary>
	/// <param name="HasSidecar">Whether the log has a sidecar, so the dialog can say why it lists none.</param>
	/// <param name="Parts">The parts that can be opened by name: <see cref="Engine.ReadParts"/>.</param>
	internal sealed record SidecarParts(bool HasSidecar, IReadOnlyList<LogPart> Parts)
	{
		public static readonly SidecarParts NoSidecar = new SidecarParts(false, Array.Empty<LogPart>());
	}
}
