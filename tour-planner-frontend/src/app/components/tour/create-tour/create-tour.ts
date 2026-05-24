import { Component, OnInit, inject, signal } from '@angular/core';
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
export class CreateTour implements OnInit {
  private geocodingService = inject(GeocodingService);
  private tourService = inject(TourService);
  
  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl('', [Validators.required]),
    transportType: new FormControl<TransportType>('foot-walking', [Validators.required]),
    waypoints: new FormArray<FormGroup>([]),
  });

  searchResults = signal<GeocodingResult[]>([]);
  isSearching = signal(false);

  get waypoints() {
    return this.form.controls.waypoints;
  }

  ngOnInit() {
    // A tour needs at least a start and a destination
    this.addWaypoint(); // Start
    this.addWaypoint(); // Destination
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
        this.isSearching.set(true);
        return this.geocodingService.search(value);
      })
    ).subscribe(results => {
      this.searchResults.set(results);
      this.isSearching.set(false);
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
    }, { emitEvent: false }); // Avoid triggering valueChanges again
    this.searchResults.set([]);
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
