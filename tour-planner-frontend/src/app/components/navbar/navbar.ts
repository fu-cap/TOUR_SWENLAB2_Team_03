import { Component, Output, EventEmitter, input, inject } from '@angular/core';
import { Navbutton } from '@/components/navbutton/navbutton';
import { TourService } from '@/shared/core/services/tour.service';

export type AppState = 'overview' | 'details' | 'edit' | 'logs' | 'user' | 'create' | 'info';

interface NavItem {
  icon: string;
  label: string;
  state: AppState;
}

@Component({
  selector: 'app-navbar',
  imports: [Navbutton],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  @Output() activeStateChange = new EventEmitter<AppState>();

  activeState = input.required<AppState>();
  public tourService = inject(TourService);

  mainNavItems: NavItem[] = [
    {icon: 'dashboard', label: 'Overview', state: 'overview'},
    {icon: 'list', label: 'Details', state: 'details'},
    {icon: 'edit', label: 'Edit', state: 'edit'},
    {icon: 'comment', label: 'Logs', state: 'logs'},
  ];

  actionNavItems: NavItem[] = [
    {icon: 'add', label: 'Create', state: 'create'},
  ];

  userItem: NavItem[] = [
    {icon: 'info', label: 'Impressum', state: 'info'},
    {icon: 'account_circle', label: 'Account', state: 'user'},
  ];

  onStateChange(state: AppState): void {
    this.activeStateChange.emit(state);
  }

  isItemDisabled(item: NavItem): boolean {
    const noTourSelected = this.tourService.selectedTour() === null;
    return noTourSelected && (item.state === 'details' || item.state === 'edit' || item.state === 'logs');
  }
}
