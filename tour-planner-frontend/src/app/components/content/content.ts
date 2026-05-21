import { Component, Input } from '@angular/core';
import { AppState } from '@/components/navbar/navbar';
import { Impressum} from '@/components/impressum/impressum';

@Component({
  selector: 'app-content',
  imports: [Impressum],
  templateUrl: './content.html',
  styleUrl: './content.css',
})
export class Content {
  @Input() activeState?: AppState;
}
