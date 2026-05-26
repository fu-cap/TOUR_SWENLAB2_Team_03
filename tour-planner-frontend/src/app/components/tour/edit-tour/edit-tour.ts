import { Component, OnInit, OnDestroy, inject, signal, computed, output } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { Tour, Waypoint, TransportType } from '@/models/tour.model';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputDirective } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { ZardSelectImports } from '@/shared/components/select';
import { ZardInputGroupComponent } from '@/shared/components/input-group';
import { ZardPopoverImports } from '@/shared/components/popover';
import { CommonModule } from '@angular/common';
import { GeocodingService, GeocodingResult } from '@/shared/core/services/geocoding.service';
import { TourService, CreateTourRequest } from '@/shared/core/services/tour.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { toast } from 'ngx-sonner';
import { AppState } from '@/components/navbar/navbar';

@Component({
  selector: 'app-edit-tour',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardIdDirective,
    ZardButtonComponent,
    ZardInputDirective,
    ...ZardFormImports,
    ...ZardSelectImports,
    ...ZardPopoverImports,
    ZardInputGroupComponent,
  ],
  templateUrl: './edit-tour.html',
  styleUrl: './edit-tour.css',
})
export class EditTour implements OnInit, OnDestroy {
  private geocodingService = inject(GeocodingService);
  private tourService = inject(TourService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);
  
  stateChange = output<AppState>();

  tour = this.tourService.selectedTour;

  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl('', [Validators.required]),
    transportType: new FormControl<TransportType>('foot-walking', [Validators.required]),
    waypoints: new FormArray<FormGroup>([]),
  });

  searchResults = signal<GeocodingResult[]>([]);
  isSearching = signal(false);
  activeWaypointIndex = signal<number | null>(null);

  routeDistance = signal<number | null>(null); // meters
  routeDuration = signal<number | null>(null); // seconds

  formattedDistance = computed(() => {
    const d = this.routeDistance();
    if (d === null) return '0.00 km';
    return (d / 1000).toFixed(2) + ' km';
  });

  formattedDuration = computed(() => {
    const s = this.routeDuration();
    if (s === null) return '0m';
    const hours = Math.floor(s / 3600);
    const minutes = Math.floor((s % 3600) / 60);
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
  });

  get waypoints() {
    return this.form.controls.waypoints;
  }

  ngOnInit() {
    const currentTour = this.tour();
    if (currentTour) {
      // Pre-fill form
      this.form.patchValue({
        name: currentTour.name,
        description: currentTour.description,
        transportType: currentTour.transportType
      });

      // Clear default waypoints and add ones from tour
      this.waypoints.clear();
      currentTour.waypoints.forEach(wp => {
        this.addWaypoint(undefined, wp);
      });

      // Initial map sync
      this.syncMapMarkers();
      this.syncRoute();
    }

    // Listen to form changes
    this.form.valueChanges.pipe(debounceTime(500)).subscribe(() => {
      this.syncMapMarkers();
      this.syncRoute();
    });
  }

  ngOnDestroy() {
    this.mapService.clearMap();
  }

  syncMapMarkers() {
    const markers: MapMarker[] = this.waypoints.controls
      .map((control, index) => {
        const val = control.value;
        if (val.lat && val.lng) {
          return {
            id: index,
            lat: val.lat,
            lng: val.lng,
            label: val.label
          };
        }
        return null;
      })
      .filter(m => m !== null) as MapMarker[];
    
    this.mapService.updateMarkers(markers);
  }

  syncRoute() {
    const coords = this.waypoints.controls
      .map(c => [c.value.lng, c.value.lat])
      .filter(c => c[0] && c[1]);

    if (coords.length >= 2) {
      const transportType = this.form.get('transportType')?.value as TransportType;
      this.routeService.getRoute(coords, transportType).subscribe({
        next: (geoJson) => {
          this.mapService.updateRoute(geoJson);
          if (geoJson.features && geoJson.features.length > 0) {
            const summary = geoJson.features[0].properties.summary;
            this.routeDistance.set(summary.distance);
            this.routeDuration.set(summary.duration);
          }
        },
        error: (err) => console.error('Error fetching route:', err)
      });
    } else {
      this.routeDistance.set(null);
      this.routeDuration.set(null);
    }
  }

  addWaypoint(index?: number, data?: Waypoint) {
    const waypointGroup = new FormGroup({
      label: new FormControl(data?.label || '', [Validators.required]),
      lat: new FormControl<number | null>(data?.latitude || null, [Validators.required]),
      lng: new FormControl<number | null>(data?.longitude || null, [Validators.required]),
    });

    waypointGroup.controls.label.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(value => {
        if (!value || value.length < 3) {
          this.searchResults.set([]);
          return of([]);
        }
        const currentIndex = this.waypoints.controls.indexOf(waypointGroup);
        this.activeWaypointIndex.set(currentIndex);
        this.isSearching.set(true);
        return this.geocodingService.search(value);
      })
    ).subscribe(results => {
      const currentIndex = this.waypoints.controls.indexOf(waypointGroup);
      if (this.activeWaypointIndex() === currentIndex) {
        this.searchResults.set(results);
        this.isSearching.set(false);
      }
    });

    if (index !== undefined) {
      this.waypoints.insert(index, waypointGroup);
    } else {
      this.waypoints.push(waypointGroup);
    }
  }

  selectResult(index: number, result: GeocodingResult) {
    const group = this.waypoints.at(index) as FormGroup;
    group.patchValue({
      label: result.address,
      lat: result.lat,
      lng: result.lng
    }, { emitEvent: true });
    this.searchResults.set([]);
    this.activeWaypointIndex.set(null);
  }

  removeWaypoint(index: number) {
    if (this.waypoints.length > 2) {
      this.waypoints.removeAt(index);
    }
  }

  onSubmit() {
    const currentTour = this.tour();
    if (this.form.valid && currentTour?.id) {
      const formValue = this.form.getRawValue();
      
      const request: CreateTourRequest = {
        userId: '00000000-0000-0000-0000-000000000000',
        name: formValue.name ?? '',
        description: formValue.description ?? '',
        transportType: formValue.transportType as TransportType,
        waypoints: (formValue.waypoints ?? []).map((wp: any) => ({
          label: wp.label,
          latitude: wp.lat,
          longitude: wp.lng
        }))
      };

      const loadingToast = toast.loading('Updating tour...', {
        description: `Saving changes to "${request.name}".`
      });

      this.tourService.updateTour(currentTour.id, request).subscribe({
        next: () => {
          toast.success('Tour updated!', {
            id: loadingToast,
            description: `Successfully saved "${request.name}".`
          });
          this.stateChange.emit('overview');
        },
        error: (err) => {
          console.error('Error updating tour:', err);
          toast.error('Failed to update tour', {
            id: loadingToast,
            description: 'There was an issue saving your changes. Please try again.'
          });
        }
      });
    }
  }

  cancel() {
    this.stateChange.emit('overview');
  }
}
