import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListviewTours } from './listview-tours';

describe('ListviewTours', () => {
  let component: ListviewTours;
  let fixture: ComponentFixture<ListviewTours>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListviewTours],
    }).compileComponents();

    fixture = TestBed.createComponent(ListviewTours);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
