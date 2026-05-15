import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateTour } from './create-tour';

describe('CreateTour', () => {
  let component: CreateTour;
  let fixture: ComponentFixture<CreateTour>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateTour],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateTour);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
