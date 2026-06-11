import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, FormArray } from '@angular/forms';
import { Waypoint, TransportType } from '@/models/tour.model';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputImports } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { ZardInputGroupComponent } from '@/shared/components/input-group';
import { ZardPopoverImports } from '@/shared/components/popover';
import { ZardTooltipImports } from '@/shared/components/tooltip';
import { CommonModule } from '@angular/common';
import { GeocodingService, GeocodingResult } from '@/shared/core/services/geocoding.service';
import { TourService, CreateTourRequest } from '@/shared/core/services/tour.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { AuthService } from '@/shared/core/services/auth.service';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-create-tour',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardIdDirective,
    ZardButtonComponent,
    ...ZardInputImports,
    ...ZardFormImports,
    ...ZardPopoverImports,
    ...ZardTooltipImports,
    ZardInputGroupComponent,
  ],
  templateUrl: './create-tour.html',
  styleUrl: './create-tour.css',
})
export class CreateTour implements OnInit, OnDestroy {
  private geocodingService = inject(GeocodingService);
  private tourService = inject(TourService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);
  private authService = inject(AuthService);

  transportOptions: { value: TransportType; icon: string; label: string }[] = [
    { value: 'driving-car', icon: 'directions_car', label: 'Car' },
    { value: 'driving-hgv', icon: 'local_shipping', label: 'Truck' },
    { value: 'cycling-regular', icon: 'directions_bike', label: 'Bike' },
    { value: 'cycling-road', icon: 'pedal_bike', label: 'Road Bike' },
    { value: 'foot-walking', icon: 'directions_walk', label: 'Walk' },
    { value: 'foot-hiking', icon: 'hiking', label: 'Hike' },
  ];

  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl(''),
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
    // A tour needs at least a start and a destination
    this.addWaypoint(); // Start
    this.addWaypoint(); // Destination

    // Listen to form changes to update map markers and route
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

    // Setup autocomplete for this waypoint
    waypointGroup.controls.label.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(value => {
        if (!waypointGroup.controls.label.dirty || !value || value.length < 3) {
          this.searchResults.set([]);
          return of([]);
        }

        // Find current index of this group in the array
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
    
    // Mark as pristine to hide the popover (template checks for dirty)
    group.get('label')?.markAsPristine();
    
    this.searchResults.set([]);
    this.activeWaypointIndex.set(null);
  }

  removeWaypoint(index: number) {
    if (this.waypoints.length > 2) {
      this.waypoints.removeAt(index);
    }
  }

  onSubmit() {
    if (this.form.valid) {
      const formValue = this.form.getRawValue();

      const request: CreateTourRequest = {
        userId: this.authService.currentUser()?.id ?? '',
        name: formValue.name ?? '',
        description: formValue.description ?? '',
        transportType: formValue.transportType as TransportType,
        waypoints: (formValue.waypoints ?? []).map((wp: any) => ({
          label: wp.label,
          latitude: wp.lat,
          longitude: wp.lng
        }))
      };

      const loadingToast = toast.loading('Creating tour...', {
        description: `Saving "${request.name}" to the database.`
      });

      this.tourService.createTour(request).subscribe({
        next: (response) => {
          toast.success('Tour created!', {
            id: loadingToast,
            description: `Successfully created "${response.name}".`
          });
          this.resetForm();
        },
        error: (err) => {
          console.error('Error creating tour:', err);
          toast.error('Failed to create tour', {
            id: loadingToast,
            description: 'There was an issue saving your tour. Please try again.'
          });
        }
      });
    }
  }

  private resetForm() {
    this.form.reset({
      transportType: 'foot-walking'
    });
    this.waypoints.clear();
    this.addWaypoint(); // New Start
    this.addWaypoint(); // New Destination
    this.mapService.clearMap();
    this.routeDistance.set(null);
    this.routeDuration.set(null);
  }
}
