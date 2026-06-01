import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { LogService } from '@/shared/core/services/log.service';
import { TourLog } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { CreateLog } from '@/components/log/create-log/create-log';
import { LogItemComponent } from '@/components/log/log-item/log-item';

@Component({
  selector: 'app-listview-logs',
  standalone: true,
  imports: [CommonModule, ZardButtonComponent, CreateLog, LogItemComponent],
  templateUrl: './listview-logs.html',
  styleUrl: './listview-logs.css',
})
export class ListviewLogs implements OnInit {
  private tourService = inject(TourService);
  private logService = inject(LogService);

  tour = this.tourService.selectedTour;
  logs = signal<TourLog[]>([]);
  isLoadingLogs = signal(false);
  isAddingLog = signal(false);

  ngOnInit() {
    if (this.tour()) {
      this.loadLogs();
    }
  }

  loadLogs() {
    const t = this.tour();
    if (!t?.id) return;

    this.isLoadingLogs.set(true);
    this.logService.getLogsByTourId(t.id).subscribe({
      next: (data) => {
        this.logs.set(data);
        this.isLoadingLogs.set(false);
      },
      error: (err) => {
        console.error('Error loading logs:', err);
        this.isLoadingLogs.set(false);
      }
    });
  }
}
