import { Component, ChangeDetectionStrategy, computed, input } from '@angular/core';

type BadgeKind = 'severity' | 'status';

const SEVERITY_LABELS: Record<string, string> = {
  low: 'Low',
  medium: 'Medium',
  high: 'High',
  critical: 'Critical'
};

const STATUS_LABELS: Record<string, string> = {
  open: 'Open',
  inprogress: 'In Progress',
  resolved: 'Resolved',
  closed: 'Closed'
};

@Component({
  selector: 'app-deviation-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './deviation-badge.component.html',
  styleUrl: './deviation-badge.component.css'
})
export class DeviationBadgeComponent {
  readonly label = input.required<string>();
  readonly kind = input.required<BadgeKind>();

  readonly displayLabel = computed(() => {
    const val = this.label().toLowerCase().replace(/\s+/g, '');
    if (this.kind() === 'severity') {
      return SEVERITY_LABELS[val] ?? this.label();
    } else {
      return STATUS_LABELS[val] ?? this.label();
    }
  });

  readonly cssClass = computed(() => {
    const val = this.label().toLowerCase().replace(/\s+/g, '');
    if (this.kind() === 'severity') {
      const map: Record<string, string> = {
        low: 'badge-severity-low',
        medium: 'badge-severity-medium',
        high: 'badge-severity-high',
        critical: 'badge-severity-critical'
      };
      return map[val] ?? 'badge-severity-low';
    } else {
      const map: Record<string, string> = {
        open: 'badge-status-open',
        inprogress: 'badge-status-inprogress',
        resolved: 'badge-status-resolved',
        closed: 'badge-status-closed'
      };
      return map[val] ?? 'badge-status-open';
    }
  });
}
