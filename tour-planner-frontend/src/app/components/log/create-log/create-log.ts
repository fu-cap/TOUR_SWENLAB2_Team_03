import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LogService } from '@/shared/core/services/log.service';
import { TourLog } from '@/models/tour.model';
import { ZardIdDirective } from '@/shared/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardFormImports } from '@/shared/components/form';
import { ZardInputImports } from '@/shared/components/input';
import { toast } from 'ngx-sonner';

@Component({
  selector: 'app-create-log',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardIdDirective,
    ZardButtonComponent,
    ...ZardInputImports,
    ...ZardFormImports
  ],
  templateUrl: './create-log.html',
})
export class CreateLog {
  @Input({ required: true }) tourId!: string;
  @Output() logCreated = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private logService = inject(LogService);

  form: FormGroup = this.fb.group({
    dateTime: [new Date().toISOString().substring(0, 16), Validators.required],
    comment: [''],
    difficulty: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    totalDistanceKm: [0, [Validators.required, Validators.min(0)]],
    totalTimeMin: ['00:30:00', Validators.required],
    rating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  onSubmit() {
    if (this.form.invalid) return;

    const newLog: TourLog = {
      tourId: this.tourId,
      ...this.form.value
    };

    this.logService.createLog(newLog).subscribe({
      next: () => {
        toast.success('Log created successfully');
        this.logCreated.emit();
      },
      error: (err) => {
        console.error('Error creating log:', err);
        toast.error('Failed to create log');
      }
    });
  }
}
