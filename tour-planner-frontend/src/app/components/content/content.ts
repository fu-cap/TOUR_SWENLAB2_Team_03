import { Component, Input } from '@angular/core';
import { AppState } from '@/components/navbar/navbar';
import { Impressum} from '@/components/impressum/impressum';
import { CreateTour } from '@/components/tour/create-tour/create-tour';

@Component({
  selector: 'app-content',
  imports: [Impressum, CreateTour],
  templateUrl: './content.html',
  styleUrl: './content.css',
})
export class Content {
  @Input() activeState?: AppState;
}
