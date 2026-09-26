import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { buttonWithText, enterValue, submitForm, textOf } from '../../../../testing/dom';
import { expectShownAsPlainText, InjectionPayload, resetInjection } from '../../../../testing/injection';
import { AccountApiService } from '../../../shared/services/account-api.service';
import { AuthService } from '../../../shared/services/auth.service';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let accountApiService: jasmine.SpyObj<AccountApiService>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    accountApiService = jasmine.createSpyObj<AccountApiService>('AccountApiService', ['CreateAccount']);
    accountApiService.CreateAccount.and.returnValue(of({ accessToken: 'token' }));
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['setToken']);

    TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        { provide: AccountApiService, useValue: accountApiService },
        { provide: AuthService, useValue: authService }
      ]
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    fixture = TestBed.createComponent(RegisterComponent);
    fixture.detectChanges();
  });

  /** Fill in a valid name, email and password */
  function fillInForm(password = 'Password1!'): void {
    enterValue(fixture, 'name', 'Alex');
    enterValue(fixture, 'email', 'user@example.com');
    enterValue(fixture, 'password', password);
  }

  it('disables create while the form is empty', () => {
    expect(buttonWithText(fixture, 'Create account')?.disabled).toBeTrue();
  });

  it('disables create for a password under 8 characters', () => {
    fillInForm('Pa1!');

    expect(buttonWithText(fixture, 'Create account')?.disabled).toBeTrue();
  });

  it('disables create for a password without a symbol', () => {
    fillInForm('Password1');

    expect(buttonWithText(fixture, 'Create account')?.disabled).toBeTrue();
  });

  it('enables create for a valid form', () => {
    fillInForm();

    expect(buttonWithText(fixture, 'Create account')?.disabled).toBeFalse();
  });

  it('sends the name, email and password', () => {
    fillInForm();

    submitForm(fixture);

    expect(accountApiService.CreateAccount).toHaveBeenCalledWith({ name: 'Alex', email: 'user@example.com', password: 'Password1!' });
  });

  it('stores the returned token', () => {
    fillInForm();

    submitForm(fixture);

    expect(authService.setToken).toHaveBeenCalledWith('token');
  });

  it('opens the dashboard after creating the account', () => {
    fillInForm();

    submitForm(fixture);

    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('shows the server error when the email is taken', () => {
    accountApiService.CreateAccount.and.returnValue(throwError(() => new HttpErrorResponse({ error: 'An account with that email already exists.', status: 400 })));
    fillInForm();

    submitForm(fixture);

    expect(textOf(fixture)).toContain('An account with that email already exists.');
  });

  it('shows a fallback error when the server gives no message', () => {
    accountApiService.CreateAccount.and.returnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    fillInForm();

    submitForm(fixture);

    expect(textOf(fixture)).toContain('Could not create the account.');
  });

  describe('script injection', () => {
    beforeEach(() => resetInjection());

    it('shows a server error containing HTML as plain text', async () => {
      accountApiService.CreateAccount.and.returnValue(throwError(() => new HttpErrorResponse({ error: InjectionPayload, status: 400 })));
      fillInForm();

      submitForm(fixture);

      await expectShownAsPlainText(fixture);
    });
  });
});
