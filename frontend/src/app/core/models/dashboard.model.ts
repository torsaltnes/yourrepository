import { DeviationSeverity, DeviationStatus, DeviationCategory } from './deviation.model';

// ── Lightweight deviation summary (used in dashboard recentDeviations) ────────
// Mirrors backend: Greenfield.Application.Deviations.DeviationSummaryDto
export interface DeviationSummaryDto {
  id: string;
  title: string;
  status: DeviationStatus;
  severity: DeviationSeverity;
  category: DeviationCategory;
  reportedBy: string;
  assignedTo: string | null;
  createdAt: string;  // ISO 8601 UTC
  updatedAt: string;  // ISO 8601 UTC
  dueDate: string | null;
  tags: string[];
  attachmentCount: number;
  commentCount: number;
}

// ── Monthly trend entry ───────────────────────────────────────────────────────
export interface MonthlyTrendDto {
  month: string; // e.g. "2026-01"
  count: number;
}

// ── Dashboard summary response ────────────────────────────────────────────────
// Mirrors backend: Greenfield.Application.Dashboard.DashboardSummaryDto
export interface DashboardSummaryDto {
  totalDeviations: number;
  openDeviations: number;
  overdueDeviations: number;
  byStatus: Record<string, number>;
  bySeverity: Record<string, number>;
  byCategory: Record<string, number>;
  monthlyTrend: MonthlyTrendDto[];
  recentDeviations: DeviationSummaryDto[];
}
