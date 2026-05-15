import { Component } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Tour} from '@/models/tour.model';
import { ZardIdDirective} from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputDirective } from '@/shared/components/input';
import { ZardFormImports } from '@/shared/components/form';
import { ZardSelectImports } from '@/shared/components/select';

@Component({
  selector: 'app-create-tour',
  imports: [ReactiveFormsModule, ZardIdDirective, ZardButtonComponent, ZardInputDirective, ZardFormImports, ZardSelectImports],
  templateUrl: './create-tour.html',
  styleUrl: './create-tour.css',
})
export class CreateTour {
  form = new FormGroup({
    name: new FormControl('', [Validators.required]),
    description: new FormControl('', [Validators.required]),
    transportType: new FormControl('', [Validators.required]),

  })
}
