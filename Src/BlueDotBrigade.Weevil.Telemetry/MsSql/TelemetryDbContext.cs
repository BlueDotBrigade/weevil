namespace BlueDotBrigade.Weevil.Telemetry.MsSql
{
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

		public DbSet<SessionRecord> Sessions => Set<SessionRecord>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SessionRecord>(entity =>
			{
				entity.HasKey(e => e.SessionId);

				entity.Property(e => e.Application)
					.HasMaxLength(256)
					.IsRequired();

				entity.Property(e => e.Version)
					.HasMaxLength(32)
					.IsRequired();

				entity.Property(e => e.SchemaVersion)
					.HasMaxLength(16)
					.IsRequired();
			});
		}
	}
}
