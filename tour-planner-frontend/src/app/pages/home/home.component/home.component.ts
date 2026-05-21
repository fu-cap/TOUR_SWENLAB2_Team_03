import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import * as L from 'leaflet';
import { Content} from '@/components/content/content';
import { Navbar, AppState } from '@/components/navbar/navbar';


@Component({
  selector: 'app-home',
  imports: [Navbar, Content],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit, OnDestroy {
  private map: L.Map | null = null;
  currentState = signal<AppState>('overview');

  ngOnInit() {
    this.map = L.map('map', { zoomControl: false }).setView([54, 15], 5);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);

    L.control.zoom({
      position: 'topright'
    }).addTo(this.map);
  }

  ngOnDestroy() {
    this.map?.remove();
    this.map = null;
  }
}
