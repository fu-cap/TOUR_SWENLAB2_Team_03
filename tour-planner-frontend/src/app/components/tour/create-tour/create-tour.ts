import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
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
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideTrash2, lucideMapPin, lucideSend, lucideSearch } from '@ng-icons/lucide';
import { GeocodingService, GeocodingResult } from '@/shared/core/services/geocoding.service';
import { TourService, CreateTourRequest } from '@/shared/core/services/tour.service';
import { MapService, MapMarker } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';

@Component({
  selector: 'app-create-tour',
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
    NgIcon
  ],
  providers: [
    provideIcons({ lucidePlus, lucideTrash2, lucideMapPin, lucideSend, lucideSearch })
  ],
  templateUrl: './create-tour.html',
  styleUrl: './create-tour.css',
})
export class CreateTour implements OnInit, OnDestroy {
  private geocodingService = inject(GeocodingService);
  private tourService = inject(TourService);
  private mapService = inject(MapService);
  private routeService = inject(RouteService);
  
  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl('', [Validators.required]),
    transportType: new FormControl<TransportType>('foot-walking', [Validators.required]),
    waypoints: new FormArray<FormGroup>([]),
  });

  searchResults = signal<GeocodingResult[]>([]);
  isSearching = signal(false);
  activeWaypointIndex = signal<number | null>(null);

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
            label: val.adresse
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
        next: (geoJson) => this.mapService.updateRoute(geoJson),
        error: (err) => console.error('Error fetching route:', err)
      });
    }
  }

  addWaypoint(index?: number) {
    const waypointGroup = new FormGroup({
      adresse: new FormControl('', [Validators.required]),
      lat: new FormControl<number | null>(null, [Validators.required]),
      lng: new FormControl<number | null>(null, [Validators.required]),
    });

    // Setup autocomplete for this waypoint
    waypointGroup.controls.adresse.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(value => {
        if (!value || value.length < 3) {
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
      adresse: result.address,
      lat: result.lat,
      lng: result.lng
    }, { emitEvent: true }); // Trigger valueChanges to sync map
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
        userId: '00000000-0000-0000-0000-000000000000', // Placeholder
        name: formValue.name ?? '',
        description: formValue.description ?? '',
        transportType: formValue.transportType as TransportType,
        waypoints: (formValue.waypoints ?? []).map((wp: any) => ({
          address: wp.adresse,
          latitude: wp.lat,
          longitude: wp.lng
        }))
      };

      this.tourService.createTour(request).subscribe({
        next: (response) => {
          console.log('Tour created successfully:', response);
          // TODO: Redirect or show success message
        },
        error: (err) => {
          console.error('Error creating tour:', err);
        }
      });
    }
  }
}
