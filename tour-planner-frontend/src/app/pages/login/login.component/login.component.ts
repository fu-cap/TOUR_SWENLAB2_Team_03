import { Component, OnInit, OnDestroy } from '@angular/core';
import * as L from 'leaflet';
import { Content } from '@/components/content/content';

@Component({
  selector: 'app-login.component',
  imports: [Content],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements  OnInit, OnDestroy {
  private map: L.Map | null = null;

  ngOnInit() {
    this.map = L.map('map', {
      zoomControl: false,
      dragging: false,
      scrollWheelZoom: false,
      doubleClickZoom: false,
      boxZoom: false,
      keyboard: false,
      touchZoom: false,
    }).setView([54, 15], 5);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);
  }

  ngOnDestroy() {
    this.map?.remove();
    this.map = null;
  }
}
