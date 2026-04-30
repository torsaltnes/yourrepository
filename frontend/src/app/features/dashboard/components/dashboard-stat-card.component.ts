import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type StatCardTone = 'neutral' | 'warning' | 'danger' | 'success' | 'info';

@Component({
  selector: 'app-dashboard-stat-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="@container flex flex-col gap-3 rounded-xl border border-border
             bg-surface p-4 transition-shadow duration-150
             hover:shadow-md hover:shadow-black/10"
      [class]="toneCardClass()"
    >
      <!-- Icon + tone indicator row -->
      <div class="flex items-center justify-between">
        <span
          class="inline-flex size-9 items-center justify-center rounded-lg text-lg"
          [class]="toneIconClass()"
          aria-hidden="true"
        >
          {{ icon() }}
        </span>
        <span
          class="rounded-full px-2 py-0.5 text-[0.65rem] font-medium uppercase tracking-wider"
          [class]="tonePillClass()"
        >
          {{ toneLabel() }}
        </span>
      </div>

      <!-- Value -->
      <p class="text-[2rem] font-semibold leading-none text-text-primary">
        {{ value() }}
      </p>

      <!-- Title -->
      <p class="text-caption text-text-secondary">{{ title() }}</p>
    </div>
  `,
})
export class DashboardStatCardComponent {
  readonly title = input.required<string>();
  readonly value = input.required<number | string>();
  readonly tone = input<StatCardTone>('neutral');
  readonly icon = input<string>('📋');

  protected readonly toneLabel = computed(() => {
    const map: Record<StatCardTone, string> = {
      neutral: 'Total',
      warning: 'Open',
      danger: 'Overdue',
      success: 'Closed',
      info: 'Active',
    };
    return map[this.tone()];
  });

  protected readonly toneCardClass = computed(() => {
    const map: Record<StatCardTone, string> = {
      neutral: '',
      warning: 'border-warning/30',
      danger: 'border-danger/30',
      success: 'border-success/30',
      info: 'border-primary/30',
    };
    return map[this.tone()];
  });

  protected readonly toneIconClass = computed(() => {
    const map: Record<StatCardTone, string> = {
      neutral: 'bg-surface-raised text-text-secondary',
      warning: 'bg-warning/15 text-warning',
      danger: 'bg-danger/15 text-danger',
      success: 'bg-success/15 text-success',
      info: 'bg-primary/15 text-primary',
    };
    return map[this.tone()];
  });

  protected readonly tonePillClass = computed(() => {
    const map: Record<StatCardTone, string> = {
      neutral: 'bg-surface-raised text-text-secondary',
      warning: 'bg-warning/15 text-warning',
      danger: 'bg-danger/15 text-danger',
      success: 'bg-success/15 text-success',
      info: 'bg-primary/15 text-primary',
    };
    return map[this.tone()];
  });
}
