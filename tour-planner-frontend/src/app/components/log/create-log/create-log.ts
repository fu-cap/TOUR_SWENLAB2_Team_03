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

  private getLocalIsoString(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
  }

  form: FormGroup = this.fb.group({
    startDateTime: [this.getLocalIsoString(new Date()), Validators.required],
    endDateTime: [this.getLocalIsoString(new Date(Date.now() + 30 * 60000)), Validators.required],
    pauseMinutes: [0, [Validators.required, Validators.min(0)]],
    comment: [''],
    difficulty: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    totalDistanceKm: [0, [Validators.required, Validators.min(0)]],
    rating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  get calculatedDuration(): string {
    const startVal = this.form.get('startDateTime')?.value;
    const endVal = this.form.get('endDateTime')?.value;
    const pauseVal = this.form.get('pauseMinutes')?.value ?? 0;

    if (!startVal || !endVal) {
      return '00:00:00';
    }

    const start = new Date(startVal);
    const end = new Date(endVal);

    const diffMs = end.getTime() - start.getTime();
    if (diffMs <= 0) {
      return '00:00:00';
    }

    const diffMin = Math.floor(diffMs / 60000) - pauseVal;
    if (diffMin <= 0) {
      return '00:00:00';
    }

    const hours = Math.floor(diffMin / 60);
    const minutes = diffMin % 60;
    const seconds = 0;

    const hStr = hours.toString().padStart(2, '0');
    const mStr = minutes.toString().padStart(2, '0');
    const sStr = seconds.toString().padStart(2, '0');

    return `${hStr}:${mStr}:${sStr}`;
  }

  onSubmit() {
    if (this.form.invalid || this.calculatedDuration === '00:00:00') return;

    const formValue = this.form.getRawValue();
    const newLog: TourLog = {
      tourId: this.tourId,
      dateTime: new Date(formValue.startDateTime).toISOString(),
      comment: formValue.comment || ' ',
      difficulty: formValue.difficulty,
      totalDistanceKm: formValue.totalDistanceKm,
      totalTimeMin: this.calculatedDuration,
      rating: formValue.rating
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
