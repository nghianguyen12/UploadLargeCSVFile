using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.ToTable("import_job");

            entity.HasKey(e => e.JobId);
            entity.Property(e => e.JobId).HasColumnName("job_id");

            entity.Property(e => e.FileName).HasColumnName("file_name");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.Property(e => e.TotalRows).HasColumnName("total_rows");
            entity.Property(e => e.ProcessedRows).HasColumnName("processed_rows");
            entity.Property(e => e.FailedRows).HasColumnName("failed_rows");
            entity.Property(e => e.LastProcessedRow).HasColumnName("last_processed_row");

            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
