import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { TransportType } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardBadgeComponent } from '@/shared/components/badge';

@Component({
  selector: 'app-detailsview-tour',
  standalone: true,
  imports: [CommonModule, ZardBadgeComponent],
  templateUrl: './detailsview-tour.html',
  styleUrl: './detailsview-tour.css',
})
export class DetailsviewTour implements OnInit, OnDestroy {
  private tourService = inject(TourService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);

  tour = this.tourService.selectedTour;

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
    }
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
