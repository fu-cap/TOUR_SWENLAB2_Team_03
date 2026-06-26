import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { LoginComponent } from './login.component';
import { AuthService } from '@/shared/core/services/auth.service';
import { TourService } from '@/shared/core/services/tour.service';
import { of } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal(null), login: () => of({}) } },
        { provide: TourService, useValue: { selectedTour: signal(null), getTours: () => of([]) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    // Don't call detectChanges — Leaflet tries to mount a real map
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start in login status', () => {
    expect(component.currentStatus).toBe('login');
  });
});
