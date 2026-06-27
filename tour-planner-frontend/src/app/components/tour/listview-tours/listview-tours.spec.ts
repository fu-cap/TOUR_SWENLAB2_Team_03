import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { vi } from 'vitest';

import { ListviewTours } from './listview-tours';
import { TourService } from '@/shared/core/services/tour.service';
import { AuthService } from '@/shared/core/services/auth.service';
import { ZardDialogService } from '@/shared/components/dialog';
import { Tour } from '@/models/tour.model';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

const MOCK_USER = { id: 'user-123', username: 'testuser', email: 'test@test.com' };

const MOCK_TOURS: Partial<Tour>[] = [
  {
    id: 'tour-1',
    name: 'Alpine Hike',
    description: 'A scenic alpine hike',
    waypoints: [],
    routeInformation: '',
    transportType: 'foot-hiking',
    distanceKm: 25.5,
    estimatedTime: '05:30:00',
    popularity: 3,
    childFriendliness: 6.5,
  },
  {
    id: 'tour-2',
    name: 'City Bike Tour',
    description: 'A city cycling tour',
    waypoints: [],
    routeInformation: '',
    transportType: 'cycling-regular',
    distanceKm: 80,
    estimatedTime: '04:00:00',
    popularity: 1,
    childFriendliness: 8.0,
  },
];

function makeFileEvent(jsonContent: string): Event {
  const file = new File([jsonContent], 'tours.json', { type: 'application/json' });
  const input = document.createElement('input');
  input.type = 'file';
  Object.defineProperty(input, 'files', {
    value: { 0: file, length: 1 },
    configurable: true,
  });
  return { target: input } as unknown as Event;
}

function stubFileReader(text: string) {
  return vi.spyOn(FileReader.prototype, 'readAsText').mockImplementation(function (this: FileReader) {
    Object.defineProperty(this, 'result', { value: text, configurable: true });
    (this.onload as any)?.({ target: this });
  } as any);
}

describe('ListviewTours', () => {
  let component: ListviewTours;
  let fixture: ComponentFixture<ListviewTours>;

  let tourServiceMock: Partial<TourService>;
  let authServiceMock: Partial<AuthService>;
  let dialogServiceMock: Partial<ZardDialogService>;

  beforeEach(async () => {
    const selectedTour = signal<Tour | null>(null);
    const currentUser = signal(MOCK_USER);

    tourServiceMock = {
      selectedTour,
      getTours: vi.fn().mockReturnValue(of(MOCK_TOURS as Tour[])),
      deleteTour: vi.fn().mockReturnValue(of(undefined)),
      exportTours: vi.fn().mockReturnValue(of(MOCK_TOURS as Tour[])),
      importTours: vi.fn().mockReturnValue(of(null)),
    };

    authServiceMock = { currentUser };

    dialogServiceMock = {
      create: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ListviewTours],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TourService, useValue: tourServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: ZardDialogService, useValue: dialogServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ListviewTours);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should load tours on init with the current userId', () => {
    expect(tourServiceMock.getTours).toHaveBeenCalledWith('user-123', '');
    expect(component.tours()).toEqual(MOCK_TOURS as Tour[]);
  });

  it('should set isLoading to false after tours are loaded', () => {
    expect(component.isLoading()).toBeFalsy();
  });

  it('should handle an error from getTours gracefully', async () => {
    (tourServiceMock.getTours as any) = vi.fn().mockReturnValue(throwError(() => new Error('Network error')));
    expect(() => component.loadTours()).not.toThrow();
  });

  it('should set selectedTour and emit "details" on selectTour()', () => {
    const emitSpy = vi.spyOn(component.stateChange, 'emit');
    component.selectTour(MOCK_TOURS[0] as Tour);
    expect(tourServiceMock.selectedTour!()).toEqual(MOCK_TOURS[0] as Tour);
    expect(emitSpy).toHaveBeenCalledWith('details');
  });

  it('should set selectedTour and emit "edit" on editTour()', () => {
    const emitSpy = vi.spyOn(component.stateChange, 'emit');
    component.editTour(MOCK_TOURS[1] as Tour);
    expect(tourServiceMock.selectedTour!()).toEqual(MOCK_TOURS[1] as Tour);
    expect(emitSpy).toHaveBeenCalledWith('edit');
  });

  it('should format HH:mm:ss with hours correctly', () => {
    expect(component.formatDuration('05:30:00')).toBe('5h 30m');
  });

  it('should format 00:mm:ss as minutes only', () => {
    expect(component.formatDuration('00:45:00')).toBe('45m');
  });

  it('should return "0m" for undefined duration', () => {
    expect(component.formatDuration(undefined)).toBe('0m');
  });

  it('should handle days.HH:mm:ss format correctly', () => {
    expect(component.formatDuration('1.02:30:00')).toBe('26h 30m');
  });

  it('should return "hiking" for foot-hiking', () => {
    expect(component.getTransportIcon('foot-hiking')).toBe('hiking');
  });

  it('should return "directions_bike" for cycling-regular', () => {
    expect(component.getTransportIcon('cycling-regular')).toBe('directions_bike');
  });

  it('should return "directions_walk" for foot-walking', () => {
    expect(component.getTransportIcon('foot-walking')).toBe('directions_walk');
  });

  it('should return "directions_car" for driving-car', () => {
    expect(component.getTransportIcon('driving-car')).toBe('directions_car');
  });

  it('should return "help_outline" for unknown transport type', () => {
    expect(component.getTransportIcon('unknown' as any)).toBe('help_outline');
  });

  it('should call exportTours with the current userId', () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const anchor = { href: '', download: '', click: vi.fn() } as any;
    vi.spyOn(document, 'createElement').mockReturnValue(anchor);
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => anchor);
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => anchor);

    component.onExportTours();

    expect(tourServiceMock.exportTours).toHaveBeenCalledWith('user-123');
  });

  it('should trigger a download anchor click on successful export', () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const anchor = { href: '', download: '', click: vi.fn() } as any;
    vi.spyOn(document, 'createElement').mockReturnValue(anchor);
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => anchor);
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => anchor);

    component.onExportTours();

    expect(anchor.click).toHaveBeenCalled();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:test');
  });

  it('should not throw when exportTours returns an empty array', () => {
    (tourServiceMock.exportTours as any) = vi.fn().mockReturnValue(of([]));
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const anchor = { href: '', download: '', click: vi.fn() } as any;
    vi.spyOn(document, 'createElement').mockReturnValue(anchor);
    vi.spyOn(document.body, 'appendChild').mockImplementation(() => anchor);
    vi.spyOn(document.body, 'removeChild').mockImplementation(() => anchor);

    expect(() => component.onExportTours()).not.toThrow();
  });

  it('should handle export service errors gracefully', () => {
    (tourServiceMock.exportTours as any) = vi.fn().mockReturnValue(
      throwError(() => new Error('Network error'))
    );
    expect(() => component.onExportTours()).not.toThrow();
  });

  it('should do nothing when no file is selected', () => {
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: { length: 0 }, configurable: true });
    const event = { target: input } as unknown as Event;

    component.onImportFileSelected(event);

    expect(tourServiceMock.importTours).not.toHaveBeenCalled();
  });

  it('should call importTours with userId and parsed tours for valid JSON array', () => {
    const toursJson = JSON.stringify([{ name: 'Test Tour' }]);
    const event = makeFileEvent(toursJson);
    stubFileReader(toursJson);

    component.onImportFileSelected(event);

    expect(tourServiceMock.importTours).toHaveBeenCalledWith('user-123', [{ name: 'Test Tour' }]);
  });

  it('should NOT call importTours when JSON is a non-array object', () => {
    const jsonContent = JSON.stringify({ name: 'Not an array' });
    const event = makeFileEvent(jsonContent);
    stubFileReader(jsonContent);

    component.onImportFileSelected(event);

    expect(tourServiceMock.importTours).not.toHaveBeenCalled();
  });

  it('should NOT call importTours when JSON is syntactically invalid', () => {
    const badJson = '{ broken json [[[';
    const event = makeFileEvent(badJson);
    stubFileReader(badJson);

    component.onImportFileSelected(event);

    expect(tourServiceMock.importTours).not.toHaveBeenCalled();
  });

  it('should reload tours after a successful import', () => {
    const toursJson = JSON.stringify([{ name: 'Imported Tour' }]);
    const event = makeFileEvent(toursJson);
    stubFileReader(toursJson);

    vi.mocked(tourServiceMock.getTours!).mockClear();
    component.onImportFileSelected(event);

    expect(tourServiceMock.getTours).toHaveBeenCalled();
  });

  it('should handle importTours server error without crashing', () => {
    (tourServiceMock.importTours as any) = vi.fn().mockReturnValue(
      throwError(() => ({ error: { message: 'Validation error' } }))
    );

    const toursJson = JSON.stringify([{ name: 'Bad Tour' }]);
    const event = makeFileEvent(toursJson);
    stubFileReader(toursJson);

    expect(() => component.onImportFileSelected(event)).not.toThrow();
  });

  it('should debounce search and call getTours with the search term after 300ms', async () => {
    vi.useFakeTimers();
    vi.mocked(tourServiceMock.getTours!).mockClear();
    const mockEvent = { target: { value: 'alpine' } } as unknown as Event;

    component.onSearchChange(mockEvent);
    expect(tourServiceMock.getTours).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(300);
    expect(tourServiceMock.getTours).toHaveBeenCalledWith('user-123', 'alpine');
    vi.useRealTimers();
  });

  it('should update rawSearchQuery immediately on search input', () => {
    const mockEvent = { target: { value: 'bike' } } as unknown as Event;
    component.onSearchChange(mockEvent);
    expect(component.rawSearchQuery()).toBe('bike');
  });

  it('should unsubscribe from search subject on destroy without throwing', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
