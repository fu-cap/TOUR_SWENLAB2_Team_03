import { Component, OnInit, OnDestroy, signal, inject, DestroyRef } from '@angular/core';
import * as L from 'leaflet';
import { Content} from '@/components/content/content';
import { Navbar, AppState } from '@/components/navbar/navbar';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-home',
  imports: [Navbar, Content],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit, OnDestroy {
  private map: L.Map | null = null;
  private markersLayer = L.layerGroup();
  private routeLayer = L.layerGroup();
  private destroyRef = inject(DestroyRef);
  private mapService = inject(MapService);
  
  currentState = signal<AppState>('overview');

  ngOnInit() {
    this.initMap();
    this.setupSubscriptions();
  }

  private initMap() {
    this.map = L.map('map', { zoomControl: false }).setView([48.2082, 16.3738], 13); // Default to Vienna

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);

    L.control.zoom({
      position: 'topright'
    }).addTo(this.map);

    this.markersLayer.addTo(this.map);
    this.routeLayer.addTo(this.map);
  }

  private setupSubscriptions() {
    this.mapService.markerUpdates$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(markers => this.updateMarkers(markers));

    this.mapService.clearMap$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.markersLayer.clearLayers();
        this.routeLayer.clearLayers();
      });
  }

  private updateMarkers(markers: MapMarker[]) {
    if (!this.map) return;

    this.markersLayer.clearLayers();
    const leafMarkers: L.Marker[] = [];

    markers.forEach(m => {
      const marker = L.marker([m.lat, m.lng])
        .bindPopup(m.label || `Stop ${m.id}`);
      marker.addTo(this.markersLayer);
      leafMarkers.push(marker);
    });

    if (leafMarkers.length > 0) {
      const group = L.featureGroup(leafMarkers);
      this.map.fitBounds(group.getBounds().pad(0.1));
    }
  }

  ngOnDestroy() {
    this.map?.remove();
    this.map = null;
  }
}
