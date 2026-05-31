import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourLog } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardDialogService } from '@/shared/components/dialog';
import { LogService } from '@/shared/core/services/log.service';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-log-item',
  standalone: true,
  imports: [CommonModule, ZardButtonComponent],
  templateUrl: './log-item.html',
})
export class LogItemComponent {
  @Input({ required: true }) log!: TourLog;
  @Output() logDeleted = new EventEmitter<void>();

  private dialogService = inject(ZardDialogService);
  private logService = inject(LogService);

  confirmDelete() {
    this.dialogService.create({
      zTitle: 'Delete Log',
      zDescription: 'Are you sure you want to delete this log entry? This action cannot be undone.',
      zOkText: 'Delete',
      zOkDestructive: true,
      zOnOk: () => {
        if (!this.log.id) return;
        this.logService.deleteLog(this.log.id).subscribe({
          next: () => {
            toast.success('Log deleted successfully');
            this.logDeleted.emit();
          },
          error: (err) => {
            console.error('Error deleting log:', err);
            toast.error('Failed to delete log');
          }
        });
      }
    });
  }
}
