import { Component, Input, Output, EventEmitter } from '@angular/core';
import { AppState } from '@/components/navbar/navbar';
import { Impressum} from '@/components/impressum/impressum';
import { CreateTour } from '@/components/tour/create-tour/create-tour';
import { ListviewTours } from '@/components/tour/listview-tours/listview-tours';
import { DetailsviewTour } from '@/components/tour/detailsview-tour/detailsview-tour';
import { EditTour } from '@/components/tour/edit-tour/edit-tour';

@Component({
  selector: 'app-content',
  imports: [Impressum, CreateTour, ListviewTours, DetailsviewTour, EditTour],
  templateUrl: './content.html',
  styleUrl: './content.css',
})
export class Content {
  @Input() activeState?: AppState;
  @Output() activeStateChange = new EventEmitter<AppState>();
}
