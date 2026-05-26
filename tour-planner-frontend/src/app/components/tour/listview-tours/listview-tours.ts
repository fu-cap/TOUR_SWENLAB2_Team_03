import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { Tour, TransportType } from '@/models/tour.model';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardBadgeComponent } from '@/shared/components/badge';
import { ZardDialogService } from '@/shared/components/dialog';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-listview-tours',
  standalone: true,
  imports: [CommonModule, ZardButtonComponent, ZardBadgeComponent],
  templateUrl: './listview-tours.html',
  styleUrl: './listview-tours.css',
})
export class ListviewTours implements OnInit {
  private tourService = inject(TourService);
  private dialogService = inject(ZardDialogService);
  
  tours = signal<Tour[]>([]);
  isLoading = signal(true);

  ngOnInit() {
    this.loadTours();
  }

  loadTours() {
    this.isLoading.set(true);
    this.tourService.getTours().subscribe({
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

  getTransportIcon(type: TransportType): string {
    switch (type) {
      case 'driving-car': return 'directions_car';
      case 'driving-hgv': return 'local_shipping';
      case 'cycling-regular': return 'directions_bike';
      case 'cycling-road': return 'directions_run'; // Or bike again, but maybe different
      case 'foot-walking': return 'directions_walk';
      case 'foot-hiking': return 'hiking';
      default: return 'help_outline';
    }
  }
}
