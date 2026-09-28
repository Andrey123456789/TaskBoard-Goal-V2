import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TaskStatus, TaskSummary } from '../../models/task.models';
import { STATUS_LABELS, canStart, nextStatusAction } from '../../models/task-workflow';

@Component({
  selector: 'app-task-card',
  imports: [MatCardModule, MatButtonModule, MatTooltipModule],
  templateUrl: './task-card.html',
  styleUrl: './task-card.scss',
})
export class TaskCard {
  readonly task = input.required<TaskSummary>();

  readonly opened = output<string>();
  readonly statusChangeRequested = output<{ taskId: string; target: TaskStatus }>();

  protected readonly statusLabel = computed(() => STATUS_LABELS[this.task().status]);
  protected readonly action = computed(() => nextStatusAction(this.task().status));
  protected readonly actionEnabled = computed(() => canStart(this.task()));

  protected requestStatusChange(event: Event): void {
    event.stopPropagation();

    const action = this.action();
    if (action) {
      this.statusChangeRequested.emit({ taskId: this.task().id, target: action.target });
    }
  }
}
