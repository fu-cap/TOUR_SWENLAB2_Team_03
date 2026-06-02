import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditLog } from './edit-log';

describe('EditLog', () => {
  let component: EditLog;
  let fixture: ComponentFixture<EditLog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditLog],
    }).compileComponents();

    fixture = TestBed.createComponent(EditLog);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
