import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetailsviewTour } from './detailsview-tour';

describe('DetailsviewTour', () => {
  let component: DetailsviewTour;
  let fixture: ComponentFixture<DetailsviewTour>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DetailsviewTour],
    }).compileComponents();

    fixture = TestBed.createComponent(DetailsviewTour);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
