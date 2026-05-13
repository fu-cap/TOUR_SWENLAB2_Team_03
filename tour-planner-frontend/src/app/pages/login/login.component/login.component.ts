import { Component, OnInit, OnDestroy } from '@angular/core';
import * as L from 'leaflet';
import { Content } from '@/components/content/content';
import { LoginFormComponent} from '@/pages/login/login.form.component/login.form.component';
import { LoginRegisterComponent } from '@/pages/login/login.register.component/login.register.component';
import { Footer } from '@/components/footer/footer';

@Component({
  selector: 'app-login.component',
  imports: [Content, LoginFormComponent, LoginRegisterComponent, Footer],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements  OnInit, OnDestroy {
  private map: L.Map | null = null;
  currentStatus = 'login';

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

  public handleStatusUpdate(newValue: string) {
    this.currentStatus = newValue;
  }
}
