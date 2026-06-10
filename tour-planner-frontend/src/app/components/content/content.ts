import { Component, Input, Output, EventEmitter } from '@angular/core';
import { AppState } from '@/components/navbar/navbar';
import { Impressum} from '@/components/impressum/impressum';
import { CreateTour } from '@/components/tour/create-tour/create-tour';
import { ListviewTours } from '@/components/tour/listview-tours/listview-tours';
import { DetailsviewTour } from '@/components/tour/detailsview-tour/detailsview-tour';
import { EditTour } from '@/components/tour/edit-tour/edit-tour';
import { ListviewLogs } from '@/components/log/listview-logs/listview-logs';
import { AccountComponent } from '@/components/account/account';
import { ZardButtonComponent } from '@/shared/components/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-content',
  imports: [CommonModule, Impressum, CreateTour, ListviewTours, DetailsviewTour, EditTour, ListviewLogs, AccountComponent, ZardButtonComponent],
  templateUrl: './content.html',
  styleUrl: './content.css',
  host: {
    '[class.collapsed]': 'isCollapsed',
  }
})
export class Content {
  @Input() activeState?: AppState;
  @Input() isCollapsed = false;
  @Input() showToggle = true;
  @Input() centerContent = false;
  @Output() activeStateChange = new EventEmitter<AppState>();
  @Output() toggleCollapse = new EventEmitter<void>();
}
