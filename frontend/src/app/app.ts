import { Component, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string | null;
}

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('frontend');
  protected readonly forecasts = signal<WeatherForecast[]>([]);
  private readonly http = inject(HttpClient);

  constructor()
  {
    this.http.get<WeatherForecast[]>("/weatherforecast").subscribe(data => {
      console.log(data);
      this.forecasts.set(data);
    });
  }
}
