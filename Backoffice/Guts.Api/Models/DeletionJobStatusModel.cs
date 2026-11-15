using System;

namespace Guts.Api.Models;

public class DeletionJobStatusModel
{
    public Guid JobId { get; set; }
    public string TargetType { get; set; }
    public int TargetId { get; set; }
    public string TargetLabel { get; set; }
    public string Status { get; set; }
    public long TotalRows { get; set; }
    public long DeletedRows { get; set; }
    public int Percentage { get; set; }
    public string CurrentPhase { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
