import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { AuthService } from "../services/auth.service";

/** The backend host */
export const ApiHost = '192.168.1.7:5000';

/** Attach access token when signed in for every request*/
export const apiInterceptor: HttpInterceptorFn = (request, next) => {
    const authService = inject(AuthService);
    const token = authService.isSignedIn() ? authService.getToken() : null;

    return next(request.clone({
        url: `http://${ApiHost}/${request.url}`,
        setHeaders: (token !== null) ? { Authorization: `Bearer ${token}` } : {}
    }));
};
