import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Navbutton } from './navbutton';

describe('Navbutton', () => {
  let component: Navbutton;
  let fixture: ComponentFixture<Navbutton>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Navbutton],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbutton);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
