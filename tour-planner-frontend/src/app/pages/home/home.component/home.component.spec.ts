import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { HomeComponent } from './home.component';
import { AuthService } from '@/shared/core/services/auth.service';
import { TourService } from '@/shared/core/services/tour.service';
import { MapService } from '@/shared/core/services/map.service';
import { of } from 'rxjs';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: { currentUser: signal({ id: 'u1', username: 'test', email: 'a@b.c' }) } },
        { provide: TourService, useValue: { selectedTour: signal(null), getTours: () => of([]) } },
        {
          provide: MapService,
          useValue: {
            markers$: of([]),
            route$: of(null),
            updateMarkers: () => {},
            updateRoute: () => {},
            clearMap: () => {},
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    // Don't call detectChanges — Leaflet tries to manipulate real DOM
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start with overview state', () => {
    expect(component.currentState()).toBe('overview');
  });

  it('should start uncollapsed', () => {
    expect(component.isCollapsed()).toBe(false);
  });
});
