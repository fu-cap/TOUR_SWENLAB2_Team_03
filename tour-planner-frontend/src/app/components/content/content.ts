import { Component, Input } from '@angular/core';
import { AppState } from '@/components/navbar/navbar';
import { Impressum} from '@/components/impressum/impressum';
import { CreateTour } from '@/components/tour/create-tour/create-tour';
import { ListviewTours } from '@/components/tour/listview-tours/listview-tours';

@Component({
  selector: 'app-content',
  imports: [Impressum, CreateTour, ListviewTours],
  templateUrl: './content.html',
  styleUrl: './content.css',
})
export class Content {
  @Input() activeState?: AppState;
}
