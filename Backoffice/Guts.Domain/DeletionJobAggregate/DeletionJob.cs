using System;
using System.Collections.Generic;
using Guts.Common;

namespace Guts.Domain.DeletionJobAggregate;

public class DeletionJob : AggregateRoot
{
    public const int MaxTargetLabelLength = 255;
    public const int MaxCurrentPhaseLength = 255;
    public const int MaxErrorMessageLength = 1024;

    public Guid Id { get; private set; }
    public DeletionTargetType TargetType { get; private set; }
    public int TargetId { get; private set; }
    public string TargetLabel { get; private set; }
    public DeletionJobStatus Status { get; private set; }
    public long TotalRows { get; private set; }
    public long DeletedRows { get; private set; }
    public string CurrentPhase { get; private set; }
    public string ErrorMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private DeletionJob()
    {
    }

    internal DeletionJob(
        Guid id,
        DeletionTargetType targetType,
        int targetId,
        string targetLabel,
        DateTime createdAtUtc)
    {
        Contracts.Require(Enum.IsDefined(targetType), "Invalid deletion target type.");
        Contracts.Require(targetId > 0, "Target identifier must be positive.");
        Contracts.Require(!string.IsNullOrWhiteSpace(targetLabel), "Target label cannot be empty.");

        Id = id;
        TargetType = targetType;
        TargetId = targetId;
        TargetLabel = Truncate(targetLabel.Trim(), MaxTargetLabelLength);
        Status = DeletionJobStatus.Pending;
        TotalRows = 0;
        DeletedRows = 0;
        CurrentPhase = "Queued";
        CreatedAtUtc = createdAtUtc;
    }

    public void Start(long totalRows, DateTime utcNow, string initialPhase)
    {
        Contracts.Require(Status == DeletionJobStatus.Pending, "Only a pending deletion job can be started.");
        Contracts.Require(totalRows >= 1, "Total row count must be at least 1.");
        Contracts.Require(!string.IsNullOrWhiteSpace(initialPhase), "Initial phase description cannot be empty.");

        Status = DeletionJobStatus.InProgress;
        TotalRows = totalRows;
        DeletedRows = 0;
        CurrentPhase = Truncate(initialPhase.Trim(), MaxCurrentPhaseLength);
        StartedAtUtc = utcNow;
    }

    public void ResetForResume(long remainingTotalRows, DateTime utcNow, string initialPhase)
    {
        Contracts.Require(
            Status is DeletionJobStatus.Pending or DeletionJobStatus.InProgress,
            "Only a pending or in-progress deletion job can be resumed.");
        Contracts.Require(!string.IsNullOrWhiteSpace(initialPhase), "Phase description cannot be empty.");

        Status = DeletionJobStatus.InProgress;
        TotalRows = Math.Max(1, remainingTotalRows);
        DeletedRows = 0;
        CurrentPhase = Truncate(initialPhase.Trim(), MaxCurrentPhaseLength);
        StartedAtUtc ??= utcNow;
    }

    public void RecordBatchProgress(long rowsDeletedInBatch, string currentPhase)
    {
        Contracts.Require(Status == DeletionJobStatus.InProgress, "Cannot record progress when job is not in progress.");
        Contracts.Require(rowsDeletedInBatch >= 0, "Deleted row count cannot be negative.");
        Contracts.Require(!string.IsNullOrWhiteSpace(currentPhase), "Phase description cannot be empty.");

        DeletedRows = Math.Min(TotalRows, DeletedRows + rowsDeletedInBatch);
        CurrentPhase = Truncate(currentPhase.Trim(), MaxCurrentPhaseLength);
    }

    public void Complete(DateTime utcNow)
    {
        Contracts.Require(Status == DeletionJobStatus.InProgress, "Only an in-progress deletion job can be completed.");

        Status = DeletionJobStatus.Completed;
        DeletedRows = TotalRows;
        CurrentPhase = "Completed";
        CompletedAtUtc = utcNow;
    }

    public void Fail(string errorMessage, DateTime utcNow)
    {
        Contracts.Require(
            Status is DeletionJobStatus.Pending or DeletionJobStatus.InProgress,
            "Cannot mark an already finished deletion job as failed.");
        Contracts.Require(!string.IsNullOrWhiteSpace(errorMessage), "Error message cannot be empty.");

        Status = DeletionJobStatus.Failed;
        ErrorMessage = Truncate(errorMessage.Trim(), MaxErrorMessageLength);
        CompletedAtUtc = utcNow;
    }

    public int CalculatePercentage()
    {
        if (Status == DeletionJobStatus.Completed)
        {
            return 100;
        }

        if (Status == DeletionJobStatus.Pending || TotalRows <= 0)
        {
            return 0;
        }

        long raw = (DeletedRows * 100L) / TotalRows;
        return (int)Math.Clamp(raw, 0, 99);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    protected override IEnumerable<object> GetIdComponents()
    {
        yield return Id;
    }
}
