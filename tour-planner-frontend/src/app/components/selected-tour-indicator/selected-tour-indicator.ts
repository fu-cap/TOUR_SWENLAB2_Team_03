import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TourService } from '@/shared/core/services/tour.service';
import { TransportType } from '@/models/tour.model';
import { ZardBadgeComponent } from '@/shared/components/badge';

@Component({
  selector: 'app-selected-tour-indicator',
  standalone: true,
  imports: [CommonModule, ZardBadgeComponent],
  templateUrl: './selected-tour-indicator.html',
  styleUrl: './selected-tour-indicator.css',
})
export class SelectedTourIndicator {
  private tourService = inject(TourService);
  tour = this.tourService.selectedTour;

  getTransportIcon(type?: TransportType): string {
    if (!type) return 'help_outline';
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
