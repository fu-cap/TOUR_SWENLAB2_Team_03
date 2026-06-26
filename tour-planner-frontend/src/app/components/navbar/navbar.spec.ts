import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ComponentFixture } from '@angular/core/testing';
import { Navbar } from './navbar';
import { signal } from '@angular/core';
import { TourService } from '@/shared/core/services/tour.service';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;

  beforeEach(async () => {
    const tourServiceMock = {
      selectedTour: signal(null),
      getTours: () => {},
    };

    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: TourService, useValue: tourServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;

    // Provide the required input signal
    fixture.componentRef.setInput('activeState', 'overview');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have nav items defined', () => {
    expect(component.mainNavItems.length).toBeGreaterThan(0);
  });

  it('should emit activeStateChange when onStateChange is called', () => {
    const emitted: string[] = [];
    component.activeStateChange.subscribe((v) => emitted.push(v));
    component.onStateChange('details');
    expect(emitted).toContain('details');
  });
});
