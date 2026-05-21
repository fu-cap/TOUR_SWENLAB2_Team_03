import { Component, Output, EventEmitter, signal } from '@angular/core';
import { Navbutton } from '@/components/navbutton/navbutton';

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

  activeState = signal<AppState>('overview');

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
    this.activeState.set(state);
    this.activeStateChange.emit(state);
  }
}
