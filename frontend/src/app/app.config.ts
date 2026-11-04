import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { MAT_ICON_DEFAULT_OPTIONS } from '@angular/material/icon';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { provideAppLocale } from './core/i18n/locale.providers';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAppLocale(),
    // index.html loads Material Symbols; mat-icon would otherwise use the Material Icons font.
    { provide: MAT_ICON_DEFAULT_OPTIONS, useValue: { fontSet: 'material-symbols-outlined' } },
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    // Before the first navigation, so the route guards already know whether a session exists.
    provideAppInitializer(() => inject(AuthService).restoreSession()),
    provideRouter(routes),
  ],
};
