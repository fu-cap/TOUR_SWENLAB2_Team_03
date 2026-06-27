import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { LogService } from '@/shared/core/services/log.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { TourLog } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { CreateLog } from '@/components/log/create-log/create-log';
import { EditLog } from '@/components/log/edit-log/edit-log';
import { LogItemComponent } from '@/components/log/log-item/log-item';

@Component({
  selector: 'app-listview-logs',
  standalone: true,
  imports: [CommonModule, ZardButtonComponent, CreateLog, EditLog, LogItemComponent],
  templateUrl: './listview-logs.html',
  styleUrl: './listview-logs.css',
})
export class ListviewLogs implements OnInit, OnDestroy {
  private tourService = inject(TourService);
  private logService = inject(LogService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);

  tour = this.tourService.selectedTour;
  logs = signal<TourLog[]>([]);
  isLoadingLogs = signal(false);
  isAddingLog = signal(false);
  editingLog = signal<TourLog | null>(null);

  ngOnInit() {
    if (this.tour()) {
      this.loadLogs();
      this.syncMap();
    }
  }

  ngOnDestroy() {
    this.mapService.clearMap();
  }

  syncMap() {
    const t = this.tour();
    if (!t) return;

    const markers: MapMarker[] = t.waypoints.map((wp, index) => ({
      id: index,
      lat: wp.latitude,
      lng: wp.longitude,
      label: wp.label || `Stop ${index}`
    }));
    this.mapService.updateMarkers(markers);

    const coords = t.waypoints.map(wp => [wp.longitude, wp.latitude]);
    this.routeService.getRoute(coords, t.transportType).subscribe({
      next: (geoJson) => this.mapService.updateRoute(geoJson),
      error: (err) => console.error('Error fetching route for logs:', err)
    });
  }

  loadLogs() {
    const t = this.tour();
    if (!t?.id) return;

    this.isLoadingLogs.set(true);
    this.logService.getLogsByTourId(t.id).subscribe({
      next: (data) => {
        this.logs.set(data);
        this.isLoadingLogs.set(false);
        this.tourService.getTourById(t.id!).subscribe({
          next: (updatedTour) => {
            this.tourService.selectedTour.set(updatedTour);
          },
          error: (err) => console.error('Error refreshing tour metrics:', err)
        });
      },
      error: (err) => {
        console.error('Error loading logs:', err);
        this.isLoadingLogs.set(false);
      }
    });
  }

  startEdit(log: TourLog) {
    this.editingLog.set(log);
  }

  cancelEdit() {
    this.editingLog.set(null);
  }

  onLogUpdated() {
    this.editingLog.set(null);
    this.loadLogs();
  }
}
