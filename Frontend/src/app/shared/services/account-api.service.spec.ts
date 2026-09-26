import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AccountApiService } from './account-api.service';

describe('AccountApiService', () => {
  let service: AccountApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AccountApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts a new account to account', () => {
    const request = { name: 'Alex', email: 'user@example.com', password: 'Password1!' };

    service.CreateAccount(request).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'account' }).request.body).toEqual(request);
  });

  it('posts a login to account/login', () => {
    const request = { email: 'user@example.com', password: 'Password1!' };

    service.Login(request).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'account/login' }).request.body).toEqual(request);
  });

  it('returns the access token from the response', () => {
    let accessToken: string | undefined;

    service.Login({ email: 'user@example.com', password: 'Password1!' }).subscribe(response => accessToken = response.accessToken);
    http.expectOne('account/login').flush({ accessToken: 'token' });

    expect(accessToken).toBe('token');
  });
});
