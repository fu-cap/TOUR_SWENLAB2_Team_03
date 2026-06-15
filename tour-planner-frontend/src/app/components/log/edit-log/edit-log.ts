import { Component, Input, Output, EventEmitter, inject, OnInit } from '@angular/core';
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
  selector: 'app-edit-log',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardIdDirective,
    ZardButtonComponent,
    ...ZardInputImports,
    ...ZardFormImports
  ],
  templateUrl: './edit-log.html',
})
export class EditLog implements OnInit {
  @Input({ required: true }) log!: TourLog;
  @Output() logUpdated = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private logService = inject(LogService);

  private getLocalIsoString(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
  }

  form: FormGroup = this.fb.group({
    startDateTime: ['', Validators.required],
    endDateTime: ['', Validators.required],
    pauseMinutes: [0, [Validators.required, Validators.min(0)]],
    comment: [''],
    difficulty: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    totalDistanceKm: [0, [Validators.required, Validators.min(0)]],
    rating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  ngOnInit() {
    if (this.log) {
      // Parse totalTimeMin (TimeSpan string "hh:mm:ss")
      const parts = this.log.totalTimeMin.split(':');
      const hours = parts.length > 0 ? parseInt(parts[0], 10) : 0;
      const minutes = parts.length > 1 ? parseInt(parts[1], 10) : 0;
      const seconds = parts.length > 2 ? parseInt(parts[2], 10) : 0;
      const durationMs = ((hours * 60 + minutes) * 60 + seconds) * 1000;

      const startDate = new Date(this.log.dateTime);
      const endDate = new Date(startDate.getTime() + durationMs);

      this.form.patchValue({
        startDateTime: this.getLocalIsoString(startDate),
        endDateTime: this.getLocalIsoString(endDate),
        pauseMinutes: 0,
        comment: this.log.comment?.trim() || '',
        difficulty: this.log.difficulty,
        totalDistanceKm: this.log.totalDistanceKm,
        rating: this.log.rating
      });
    }
  }

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
    if (this.form.invalid || this.calculatedDuration === '00:00:00' || !this.log.id) return;

    const formValue = this.form.getRawValue();
    const updatedLog: TourLog = {
      ...this.log,
      dateTime: new Date(formValue.startDateTime).toISOString(),
      comment: formValue.comment || ' ',
      difficulty: formValue.difficulty,
      totalDistanceKm: formValue.totalDistanceKm,
      totalTimeMin: this.calculatedDuration,
      rating: formValue.rating
    };

    const loadingToast = toast.loading('Updating log...');

    this.logService.updateLog(this.log.id, updatedLog).subscribe({
      next: () => {
        toast.success('Log updated successfully', { id: loadingToast });
        this.logUpdated.emit();
      },
      error: (err) => {
        console.error('Error updating log:', err);
        toast.error('Failed to update log', { id: loadingToast });
      }
    });
  }
}
