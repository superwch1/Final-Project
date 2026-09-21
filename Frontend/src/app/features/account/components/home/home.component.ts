import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

const TokenStorageKey = 'accessToken';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.component.html'
})
export class HomeComponent {

}
