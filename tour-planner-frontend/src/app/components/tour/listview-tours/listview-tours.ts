import { Component, OnInit, OnDestroy, inject, signal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { AuthService } from '@/shared/core/services/auth.service';
import { Tour, TransportType } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardDialogService } from '@/shared/components/dialog';
import { ZardInputImports } from '@/shared/components/input';
import { ZardInputGroupComponent } from '@/shared/components/input-group';
import { toast } from 'ngx-sonner';
import { AppState } from '@/components/navbar/navbar';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-listview-tours',
  standalone: true,
  imports: [
    CommonModule, 
    ZardButtonComponent,
    ...ZardInputImports,
    ZardInputGroupComponent
  ],
  templateUrl: './listview-tours.html',
  styleUrl: './listview-tours.css',
})
export class ListviewTours implements OnInit, OnDestroy {
  private tourService = inject(TourService);
  private dialogService = inject(ZardDialogService);
  private authService = inject(AuthService);

  stateChange = output<AppState>();

  tours = signal<Tour[]>([]);
  isLoading = signal(true);

  searchQuery = signal<string>('');
  rawSearchQuery = signal<string>('');
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  ngOnInit() {
    this.tourService.selectedTour.set(null);
    this.loadTours();

    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(query => {
      this.searchQuery.set(query);
      this.loadTours();
    });
  }

  ngOnDestroy() {
    this.searchSubscription?.unsubscribe();
  }

  onSearchChange(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.rawSearchQuery.set(value);
    this.searchSubject.next(value);
  }

  loadTours() {
    this.isLoading.set(true);
    const userId = this.authService.currentUser()?.id ?? '';
    const search = this.searchQuery();
    this.tourService.getTours(userId, search).subscribe({
      next: (data) => {
        this.tours.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading tours:', err);
        this.isLoading.set(false);
      }
    });
  }

  confirmDelete(tour: Tour) {
    if (!tour.id) return;

    this.dialogService.create({
      zTitle: 'Delete Tour',
      zDescription: `Are you sure you want to delete "${tour.name}"? This action cannot be undone.`,
      zOkText: 'Delete',
      zOkDestructive: true,
      zOnOk: () => {
        this.tourService.deleteTour(tour.id!).subscribe({
          next: () => {
            toast.success('Tour deleted', {
              description: `Successfully deleted "${tour.name}".`
            });
            this.loadTours();
          },
          error: (err) => {
            console.error('Error deleting tour:', err);
            toast.error('Failed to delete tour');
          }
        });
      }
    });
  }

  selectTour(tour: Tour) {
    this.tourService.selectedTour.set(tour);
    this.stateChange.emit('details');
  }

  editTour(tour: Tour) {
    this.tourService.selectedTour.set(tour);
    this.stateChange.emit('edit');
  }

  formatDuration(timeSpan?: string): string {
    if (!timeSpan) return '0m';
    
    // TimeSpan often comes as "HH:mm:ss" or "days.HH:mm:ss"
    const parts = timeSpan.split(':');
    if (parts.length >= 2) {
      const hoursPart = parts[0];
      const minutes = parts[1];
      
      // Handle potential "days.hours" in the first part
      if (hoursPart.includes('.')) {
        const dayParts = hoursPart.split('.');
        const days = parseInt(dayParts[0]);
        const hours = parseInt(dayParts[1]);
        const totalHours = (days * 24) + hours;
        return `${totalHours}h ${minutes}m`;
      }
      
      const hours = parseInt(hoursPart);
      return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
    }
    
    return timeSpan;
  }

  onImportFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const reader = new FileReader();

    reader.onload = (e) => {
      try {
        const text = e.target?.result as string;
        const tours = JSON.parse(text);
        
        if (!Array.isArray(tours)) {
          toast.error('Import failed', {
            description: 'JSON file must contain an array of tours.'
          });
          input.value = '';
          return;
        }

        const userId = this.authService.currentUser()?.id ?? '';
        const loadingToast = toast.loading('Importing tours...');

        this.tourService.importTours(userId, tours).subscribe({
          next: () => {
            toast.success('Import successful', {
              id: loadingToast,
              description: 'Your tours have been imported successfully.'
            });
            this.loadTours();
            input.value = '';
          },
          error: (err) => {
            console.error('Import failed:', err);
            toast.error('Import failed', {
              id: loadingToast,
              description: err.error?.message || 'Could not import tours.'
            });
            input.value = '';
          }
        });
      } catch (err) {
        console.error('Failed to parse JSON:', err);
        toast.error('Import failed', {
          description: 'The selected file is not a valid JSON.'
        });
        input.value = '';
      }
    };

    reader.onerror = () => {
      toast.error('Failed to read file');
      input.value = '';
    };

    reader.readAsText(file);
  }

  onExportTours() {
    const userId = this.authService.currentUser()?.id ?? '';
    const loadingToast = toast.loading('Preparing export...');

    this.tourService.exportTours(userId).subscribe({
      next: (tours) => {
        try {
          const jsonString = JSON.stringify(tours, null, 2);
          const blob = new Blob([jsonString], { type: 'application/json' });
          const url = URL.createObjectURL(blob);
          
          const a = document.createElement('a');
          a.href = url;
          a.download = `tours_export_${new Date().toISOString().slice(0, 10)}.json`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          URL.revokeObjectURL(url);

          toast.success('Export successful', {
            id: loadingToast,
            description: `Exported ${tours.length} tours.`
          });
        } catch (err) {
          console.error('Export failed:', err);
          toast.error('Export failed', {
            id: loadingToast,
            description: 'Could not create JSON file.'
          });
        }
      },
      error: (err) => {
        console.error('Export failed:', err);
        toast.error('Export failed', {
          id: loadingToast,
          description: 'Failed to retrieve tours from server.'
        });
      }
    });
  }

  getTransportIcon(type: TransportType): string {
    switch (type) {
      case 'driving-car': return 'directions_car';
      case 'driving-hgv': return 'local_shipping';
      case 'cycling-regular': return 'directions_bike';
      case 'cycling-road': return 'directions_run';
      case 'foot-walking': return 'directions_walk';
      case 'foot-hiking': return 'hiking';
      default: return 'help_outline';
    }
  }
}
