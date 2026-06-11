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

  form: FormGroup = this.fb.group({
    dateTime: ['', Validators.required],
    comment: [''],
    difficulty: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    totalDistanceKm: [0, [Validators.required, Validators.min(0)]],
    totalTimeMin: ['', Validators.required],
    rating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
  });

  ngOnInit() {
    if (this.log) {
      this.form.patchValue({
        dateTime: this.log.dateTime.substring(0, 16), // Format for datetime-local
        comment: this.log.comment,
        difficulty: this.log.difficulty,
        totalDistanceKm: this.log.totalDistanceKm,
        totalTimeMin: this.log.totalTimeMin,
        rating: this.log.rating
      });
    }
  }

  onSubmit() {
    if (this.form.invalid || !this.log.id) return;

    const updatedLog: TourLog = {
      ...this.log,
      ...this.form.value
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
