using System.ComponentModel.DataAnnotations;

public class ImportJob
{
    [Key]
    public Guid JobId { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public int TotalRows { get; set; } = 0;
    public int ProcessedRows { get; set; } = 0;
    public int FailedRows { get; set; } = 0;
    public int LastProcessedRow { get; set; } = 0;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}