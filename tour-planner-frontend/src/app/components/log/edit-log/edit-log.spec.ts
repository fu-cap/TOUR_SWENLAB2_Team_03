import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture } from '@angular/core/testing';
import { EditLog } from './edit-log';
import { LogService } from '@/shared/core/services/log.service';
import { TourLog } from '@/models/tour.model';
import { of } from 'rxjs';

const MOCK_LOG: TourLog = {
  id: 'log-1',
  tourId: 'tour-1',
  dateTime: '2024-01-01T10:00:00',
  comment: 'Test comment',
  difficulty: 3,
  totalDistanceKm: 10,
  totalTimeMin: '01:30:00',
  rating: 4,
  createdAt: '2024-01-01T00:00:00',
};

describe('EditLog', () => {
  let component: EditLog;
  let fixture: ComponentFixture<EditLog>;

  beforeEach(async () => {
    const logServiceMock = {
      updateLog: () => of(MOCK_LOG),
    };

    await TestBed.configureTestingModule({
      imports: [EditLog],
      providers: [
        provideHttpClient(),
        { provide: LogService, useValue: logServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EditLog);
    component = fixture.componentInstance;
    component.log = MOCK_LOG;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with log values on ngOnInit', () => {
    component.ngOnInit();
    expect(component.form.get('comment')?.value).toBe('Test comment');
    expect(component.form.get('difficulty')?.value).toBe(3);
    expect(component.form.get('rating')?.value).toBe(4);
  });

  it('should have a cancel EventEmitter defined', () => {
    // The cancel output is a proper EventEmitter — verify it's callable
    expect(component.cancel).toBeDefined();
    expect(typeof component.cancel.emit).toBe('function');
  });
});
