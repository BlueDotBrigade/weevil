namespace BlueDotBrigade.Weevil.Data.SqlClient
{
	using System;
	using BlueDotBrigade.Weevil.Diagnostics;
	using Microsoft.EntityFrameworkCore;

	/// <summary>
	/// Entity Framework <see cref="DbContext"/> for the Weevil telemetry schema.
	/// This context is insert-only; no read, update, or delete operations are performed.
	/// </summary>
	internal sealed class TelemetryDbContext : DbContext
	{
		public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
			: base(options)
		{
		}

		public DbSet<TelemetrySession> Sessions => Set<TelemetrySession>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TelemetrySession>(entity =>
			{
             entity.ToTable("Session", "dbo");

				entity.HasKey(e => e.SessionId);

				entity.Property(e => e.Application)
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.Source)
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.Version)
					.HasConversion(
						v => v != null ? v.ToString() : "0.0",
						s => string.IsNullOrEmpty(s) ? new Version(0, 0) : new Version(s))
					.HasMaxLength(32)
					.IsRequired();

				entity.Property(e => e.SchemaVersion)
					.HasMaxLength(16)
					.IsRequired();

				entity.Property(e => e.InstalledCpu)
					.HasMaxLength(256)
					.IsRequired();
			});
		}
	}
}
