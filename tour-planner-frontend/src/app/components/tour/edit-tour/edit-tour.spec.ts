import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { EditTour } from './edit-tour';
import { AuthService } from '@/shared/core/services/auth.service';
import { TourService } from '@/shared/core/services/tour.service';
import { MapService } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { GeocodingService } from '@/shared/core/services/geocoding.service';
import { of } from 'rxjs';

const mockTour = {
  id: 't1', name: 'Test', description: '', waypoints: [],
  routeInformation: '', transportType: 'foot-hiking' as const,
};

describe('EditTour', () => {
  let component: EditTour;
  let fixture: ComponentFixture<EditTour>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditTour],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: { currentUser: signal({ id: 'u1', username: 'test', email: 'a@b.c' }) } },
        { provide: TourService, useValue: { selectedTour: signal(mockTour), updateTour: () => of({}), getTours: () => of([]) } },
        { provide: MapService, useValue: { updateMarkers: () => {}, updateRoute: () => {}, clearMap: () => {} } },
        { provide: RouteService, useValue: { getRoute: () => of({}) } },
        { provide: GeocodingService, useValue: { search: () => of([]) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EditTour);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have transport options defined', () => {
    expect(component.transportOptions.length).toBeGreaterThan(0);
  });
});
