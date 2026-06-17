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

		public DbSet<TelemetrySessionMetric> SessionMetrics => Set<TelemetrySessionMetric>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<TelemetrySession>(entity =>
			{
				entity.ToTable("telemetry_session", "dbo");

				entity.HasKey(e => e.SessionId);

				entity.Property(e => e.SessionId)
					.HasColumnName("session_id")
					.ValueGeneratedNever();

				entity.Property(e => e.Application)
					.HasColumnName("application")
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.Source)
					.HasColumnName("source")
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.Version)
					.HasColumnName("version")
					.HasConversion(
						v => v != null ? v.ToString() : "0.0",
						s => string.IsNullOrEmpty(s) ? new Version(0, 0) : new Version(s))
					.HasMaxLength(32)
					.IsRequired();

				entity.Property(e => e.IsDebugging)
					.HasColumnName("is_debugging");

				entity.Property(e => e.SessionStartUtc)
					.HasColumnName("session_start_utc");

				entity.Property(e => e.SessionEndUtc)
					.HasColumnName("session_end_utc");

				entity.Property(e => e.SessionActiveMinutes)
					.HasColumnName("session_active_minutes");

				entity.Property(e => e.LogFileSizeBytes)
					.HasColumnName("log_file_size_bytes");

				entity.Property(e => e.InstalledRamMb)
					.HasColumnName("installed_ram_mb");

				entity.Property(e => e.InstalledCpu)
					.HasColumnName("installed_cpu")
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.SchemaVersion)
					.HasColumnName("schema_version")
					.HasMaxLength(16)
					.IsRequired();

				entity.HasMany(e => e.Metrics)
					.WithOne(m => m.Session)
					.HasForeignKey(m => m.SessionId);
			});

			modelBuilder.Entity<TelemetrySessionMetric>(entity =>
			{
				entity.ToTable("telemetry_session_metric", "dbo");

				entity.HasKey(e => new { e.SessionId, e.MetricKey });

				entity.Property(e => e.SessionId)
					.HasColumnName("session_id");

				entity.Property(e => e.MetricKey)
					.HasColumnName("metric_key")
					.HasMaxLength(128)
					.IsRequired();

				entity.Property(e => e.MetricCount)
					.HasColumnName("metric_count");
			});
		}
	}
}
