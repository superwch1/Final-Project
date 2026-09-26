import { TestBed } from '@angular/core/testing';
import { fakeToken, secondsFromNow } from '../../../testing/test-data';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  const validToken = fakeToken({ sub: 'account-1', email: 'user@example.com', name: 'Alex', exp: secondsFromNow(3600) });

  function createService(storedToken: string | null = null): AuthService {
    if (storedToken !== null) {
      localStorage.setItem('accessToken', storedToken);
    }

    return TestBed.inject(AuthService);
  }

  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('is signed out when no token is stored', () => {
    expect(createService().isSignedIn()).toBeFalse();
  });

  it('is signed in when an unexpired token is stored', () => {
    expect(createService(validToken).isSignedIn()).toBeTrue();
  });

  it('is signed out when the stored token has expired', () => {
    const expiredToken = fakeToken({ sub: 'account-1', exp: secondsFromNow(-60) });

    expect(createService(expiredToken).isSignedIn()).toBeFalse();
  });

  it('is signed out when the stored token is malformed', () => {
    expect(createService('not-a-token').isSignedIn()).toBeFalse();
  });

  it('returns the name from the token', () => {
    expect(createService(validToken).name()).toBe('Alex');
  });

  it('returns the account ID from the token', () => {
    expect(createService(validToken).accountId()).toBe('account-1');
  });

  it('returns no name when signed out', () => {
    expect(createService().name()).toBeNull();
  });

  it('signs in after a token is set', () => {
    const service = createService();

    service.setToken(validToken);

    expect(service.isSignedIn()).toBeTrue();
  });

  it('stores the token that is set', () => {
    const service = createService();

    service.setToken(validToken);

    expect(service.getToken()).toBe(validToken);
  });

  it('signs out after signOut', () => {
    const service = createService(validToken);

    service.signOut();

    expect(service.isSignedIn()).toBeFalse();
  });

  it('removes the stored token after signOut', () => {
    const service = createService(validToken);

    service.signOut();

    expect(localStorage.getItem('accessToken')).toBeNull();
  });
});
