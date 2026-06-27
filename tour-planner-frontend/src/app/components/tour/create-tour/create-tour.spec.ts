import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { CreateTour } from './create-tour';
import { AuthService } from '@/shared/core/services/auth.service';
import { TourService } from '@/shared/core/services/tour.service';
import { MapService } from '@/shared/core/services/map.service';
import { RouteService } from '@/shared/core/services/route.service';
import { GeocodingService } from '@/shared/core/services/geocoding.service';
import { of } from 'rxjs';

describe('CreateTour', () => {
  let component: CreateTour;
  let fixture: ComponentFixture<CreateTour>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateTour],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: { currentUser: signal({ id: 'u1', username: 'test', email: 'a@b.c' }) } },
        { provide: TourService, useValue: { selectedTour: signal(null), createTour: () => of({}), getTours: () => of([]) } },
        { provide: MapService, useValue: { updateMarkers: () => {}, updateRoute: () => {}, clearMap: () => {} } },
        { provide: RouteService, useValue: { getRoute: () => of({}) } },
        { provide: GeocodingService, useValue: { search: () => of([]) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateTour);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have at least 2 transport options', () => {
    expect(component.transportOptions.length).toBeGreaterThanOrEqual(2);
  });
});
