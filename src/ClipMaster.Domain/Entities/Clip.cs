namespace ClipMaster.Domain.Entities;

public class Clip
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int DurationSeconds { get; set; }
    public string OutputPath { get; set; } = string.Empty;
    public bool IsPartOfHybrid { get; set; }
}
