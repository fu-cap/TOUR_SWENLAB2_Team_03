import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListviewLogs } from './listview-logs';

describe('ListviewLogs', () => {
  let component: ListviewLogs;
  let fixture: ComponentFixture<ListviewLogs>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListviewLogs],
    }).compileComponents();

    fixture = TestBed.createComponent(ListviewLogs);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
