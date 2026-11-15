export type DeletionTargetType = "Course" | "Period";

export type DeletionJobStatus =
  | "Pending"
  | "InProgress"
  | "Completed"
  | "Failed";

export interface IDeletionJobStatusModel {
  jobId: string;
  targetType: DeletionTargetType;
  targetId: number;
  targetLabel: string;
  status: DeletionJobStatus;
  totalRows: number;
  deletedRows: number;
  percentage: number;
  currentPhase: string;
  errorMessage?: string | null;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
}
