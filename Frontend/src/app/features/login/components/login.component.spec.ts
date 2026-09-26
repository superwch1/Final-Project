import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { buttonWithText, enterValue, submitForm, textOf } from '../../../../testing/dom';
import { expectShownAsPlainText, InjectionPayload, resetInjection } from '../../../../testing/injection';
import { AccountApiService } from '../../../shared/services/account-api.service';
import { AuthService } from '../../../shared/services/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let accountApiService: jasmine.SpyObj<AccountApiService>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    accountApiService = jasmine.createSpyObj<AccountApiService>('AccountApiService', ['Login']);
    accountApiService.Login.and.returnValue(of({ accessToken: 'token' }));
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['setToken']);

    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AccountApiService, useValue: accountApiService },
        { provide: AuthService, useValue: authService }
      ]
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
  });

  function fillInForm(): void {
    enterValue(fixture, 'email', 'user@example.com');
    enterValue(fixture, 'password', 'Password1!');
  }

  it('disables sign in while the form is empty', () => {
    expect(buttonWithText(fixture, 'Sign in')?.disabled).toBeTrue();
  });

  it('disables sign in for an invalid email', () => {
    enterValue(fixture, 'email', 'not-an-email');
    enterValue(fixture, 'password', 'Password1!');

    expect(buttonWithText(fixture, 'Sign in')?.disabled).toBeTrue();
  });

  it('sends the email and password', () => {
    fillInForm();

    submitForm(fixture);

    expect(accountApiService.Login).toHaveBeenCalledWith({ email: 'user@example.com', password: 'Password1!' });
  });

  it('stores the returned token', () => {
    fillInForm();

    submitForm(fixture);

    expect(authService.setToken).toHaveBeenCalledWith('token');
  });

  it('opens the dashboard after signing in', () => {
    fillInForm();

    submitForm(fixture);

    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('shows the server error when sign in fails', () => {
    accountApiService.Login.and.returnValue(throwError(() => new HttpErrorResponse({ error: 'Invalid email or password.', status: 401 })));
    fillInForm();

    submitForm(fixture);

    expect(textOf(fixture)).toContain('Invalid email or password.');
  });

  it('shows a fallback error when the server gives no message', () => {
    accountApiService.Login.and.returnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    fillInForm();

    submitForm(fixture);

    expect(textOf(fixture)).toContain('Could not sign in.');
  });

  it('does not send an empty form', () => {
    submitForm(fixture);

    expect(accountApiService.Login).not.toHaveBeenCalled();
  });

  describe('script injection', () => {
    beforeEach(() => resetInjection());

    it('shows a server error containing HTML as plain text', async () => {
      accountApiService.Login.and.returnValue(throwError(() => new HttpErrorResponse({ error: InjectionPayload, status: 401 })));
      fillInForm();

      submitForm(fixture);

      await expectShownAsPlainText(fixture);
    });
  });
});
