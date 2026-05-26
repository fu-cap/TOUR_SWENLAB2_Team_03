import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditTour } from './edit-tour';

describe('EditTour', () => {
  let component: EditTour;
  let fixture: ComponentFixture<EditTour>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditTour],
    }).compileComponents();

    fixture = TestBed.createComponent(EditTour);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
