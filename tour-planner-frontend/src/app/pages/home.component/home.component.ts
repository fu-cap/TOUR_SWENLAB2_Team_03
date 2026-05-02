import { Component, OnInit, OnDestroy } from '@angular/core';
import * as L from 'leaflet';
import { ZardButtonComponent} from '@/shared/components/button';

@Component({
  selector: 'app-home.component',
  imports: [ZardButtonComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit, OnDestroy {
  private map: L.Map | null = null;

  ngOnInit() {
    this.map = L.map('map', { zoomControl: false }).setView([54, 15], 5);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);

    L.control.zoom({
      position: 'bottomright'
    }).addTo(this.map);
  }

  ngOnDestroy() {
    this.map?.remove();
    this.map = null;
  }
}
