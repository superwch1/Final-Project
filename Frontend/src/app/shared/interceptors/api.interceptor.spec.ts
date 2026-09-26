import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../services/auth.service';
import { apiInterceptor, ApiHost } from './api.interceptor';

describe('apiInterceptor', () => {
  let httpClient: HttpClient;
  let http: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['isSignedIn', 'getToken']);
    authService.getToken.and.returnValue('token');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the request to the backend host', () => {
    httpClient.get('room').subscribe();

    expect(http.expectOne(`http://${ApiHost}/room`)).toBeTruthy();
  });

  it('attaches the access token when signed in', () => {
    authService.isSignedIn.and.returnValue(true);

    httpClient.get('room').subscribe();

    expect(http.expectOne(`http://${ApiHost}/room`).request.headers.get('Authorization')).toBe('Bearer token');
  });

  it('sends no access token when signed out', () => {
    authService.isSignedIn.and.returnValue(false);

    httpClient.get('room').subscribe();

    expect(http.expectOne(`http://${ApiHost}/room`).request.headers.has('Authorization')).toBeFalse();
  });
});
