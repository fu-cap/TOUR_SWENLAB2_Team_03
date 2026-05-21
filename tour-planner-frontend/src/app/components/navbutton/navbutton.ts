import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardTooltipImports } from '@/shared/components/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { AppState } from '@/components/navbar/navbar';

@Component({
  selector: 'app-nav-button',
  imports: [ZardButtonComponent, ZardTooltipImports, MatIconModule],
  templateUrl: './navbutton.html',
  styleUrl: './navbutton.css',
})
export class Navbutton {
  @Input({ required: true }) icon!: string;
  @Input({ required: true }) label!: string;
  @Input({ required: true }) state!: AppState;
  @Input() isActive = false;

  @Output() stateChange = new EventEmitter<AppState>();

  onClick(): void {
    this.stateChange.emit(this.state);
  }
}
