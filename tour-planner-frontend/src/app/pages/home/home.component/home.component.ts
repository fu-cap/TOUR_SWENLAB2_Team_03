import { Component, OnInit, OnDestroy, signal, inject, DestroyRef, effect } from '@angular/core';
import * as L from 'leaflet';
import { Content} from '@/components/content/content';
import { Navbar, AppState } from '@/components/navbar/navbar';
import { SelectedTourIndicator } from '@/components/selected-tour-indicator/selected-tour-indicator';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-home',
  imports: [Navbar, Content, SelectedTourIndicator],
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
  isCollapsed = signal(false);

  private readonly EUROPE_CENTER: L.LatLngExpression = [50, 10];
  private readonly EUROPE_ZOOM = 5;

  constructor() {
    // Automatically reset map when switching back to overview
    effect(() => {
      if (this.currentState() === 'overview' && this.map) {
        this.resetToDefaultView();
      }
    });
  }

  ngOnInit() {
    this.initMap();
    this.setupSubscriptions();
  }

  private initMap() {
    this.map = L.map('map', { zoomControl: false }).setView(this.EUROPE_CENTER, this.EUROPE_ZOOM);

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

  private resetToDefaultView() {
    if (!this.map) return;
    this.markersLayer.clearLayers();
    this.routeLayer.clearLayers();
    this.map.setView(this.EUROPE_CENTER, this.EUROPE_ZOOM);
  }

  private setupSubscriptions() {
    this.mapService.markerUpdates$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(markers => this.updateMarkers(markers));

    this.mapService.routeUpdates$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(geoJson => this.updateRoute(geoJson));

    this.mapService.clearMap$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.markersLayer.clearLayers();
        this.routeLayer.clearLayers();
      });
  }

  private updateRoute(geoJson: any) {
    if (!this.map) return;

    this.routeLayer.clearLayers();
    const route = L.geoJSON(geoJson, {
      style: {
        color: '#3b82f6', // primary blue
        weight: 5,
        opacity: 0.7
      }
    });
    route.addTo(this.routeLayer);
  }

  private updateMarkers(markers: MapMarker[]) {
    if (!this.map) return;

    this.markersLayer.clearLayers();
    const leafMarkers: L.Marker[] = [];

    const customIcon = L.divIcon({
      className: 'custom-div-icon',
      html: "<div style='background-color:#3b82f6; width:0.75rem; height:0.75rem; border:0.125rem solid white; border-radius:50%; box-shadow:0 0 0.25rem rgba(0,0,0,0.5);'></div>",
      iconSize: [12, 12],
      iconAnchor: [6, 6]
    });

    markers.forEach(m => {
      const marker = L.marker([m.lat, m.lng], { icon: customIcon })
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
