import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { LogService } from '@/shared/core/services/log.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { TourLog, TransportType } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardBadgeComponent } from '@/shared/components/badge';
import { CreateLog } from '@/components/log/create-log/create-log';
import { LogItemComponent } from '@/components/log/log-item/log-item';

@Component({
  selector: 'app-detailsview-tour',
  standalone: true,
  imports: [CommonModule, ZardBadgeComponent, ZardButtonComponent, CreateLog, LogItemComponent],
  templateUrl: './detailsview-tour.html',
  styleUrl: './detailsview-tour.css',
})
export class DetailsviewTour implements OnInit, OnDestroy {
  private tourService = inject(TourService);
  private logService = inject(LogService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);

  tour = this.tourService.selectedTour;
  logs = signal<TourLog[]>([]);
  isLoadingLogs = signal(false);
  isAddingLog = signal(false);

  formattedDuration = computed(() => {
    const timeSpan = this.tour()?.estimatedTime;
    if (!timeSpan) return '0m';

    const parts = timeSpan.split(':');
    if (parts.length >= 2) {
      const hoursPart = parts[0];
      const minutes = parts[1];

      if (hoursPart.includes('.')) {
        const dayParts = hoursPart.split('.');
        const days = parseInt(dayParts[0]);
        const hours = parseInt(dayParts[1]);
        const totalHours = (days * 24) + hours;
        return `${totalHours}h ${minutes}m`;
      }

      const hours = parseInt(hoursPart);
      return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
    }
    return timeSpan;
  });

  ngOnInit() {
    if (this.tour()) {
      this.syncMap();
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

  ngOnDestroy() {
    this.mapService.clearMap();
  }

  syncMap() {
    const t = this.tour();
    if (!t) return;

    // 1. Show Markers
    const markers: MapMarker[] = t.waypoints.map((wp, index) => ({
      id: index,
      lat: wp.latitude,
      lng: wp.longitude,
      label: wp.label || `Stop ${index}`
    }));
    this.mapService.updateMarkers(markers);

    // 2. Fetch and Show Route
    const coords = t.waypoints.map(wp => [wp.longitude, wp.latitude]);
    this.routeService.getRoute(coords, t.transportType).subscribe({
      next: (geoJson) => this.mapService.updateRoute(geoJson),
      error: (err) => console.error('Error fetching route for details:', err)
    });
  }

  getTransportIcon(type?: TransportType): string {
    if (!type) return 'help_outline';
    switch (type) {
      case 'driving-car': return 'directions_car';
      case 'driving-hgv': return 'local_shipping';
      case 'cycling-regular': return 'directions_bike';
      case 'cycling-road': return 'directions_run';
      case 'foot-walking': return 'directions_walk';
      case 'foot-hiking': return 'hiking';
      default: return 'help_outline';
    }
  }
}
