import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { safeReturnUrl } from '../../../core/auth/auth.guards';
import { AuthService } from '../../../core/auth/auth.service';
import { LoginFailure, loginTexts } from '../login.texts';

@Component({
  selector: 'lm-login-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly texts = loginTexts;
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    rememberMe: [false],
  });
  protected readonly submitting = signal(false);
  protected readonly failure = signal<LoginFailure | null>(null);
  protected readonly passwordVisible = signal(false);
  protected readonly forgotPasswordHintVisible = signal(false);

  protected togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  protected toggleForgotPasswordHint(): void {
    this.forgotPasswordHintVisible.update((visible) => !visible);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const credentials = this.form.getRawValue();
    this.submitting.set(true);
    this.failure.set(null);

    this.auth
      .login({ ...credentials, email: credentials.email.trim() })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          void this.router.navigateByUrl(safeReturnUrl(returnUrl));
        },
        error: (error: unknown) => this.failure.set(toLoginFailure(error)),
      });
  }
}

function toLoginFailure(error: unknown): LoginFailure {
  if (error instanceof HttpErrorResponse) {
    switch (error.status) {
      case HttpStatusCode.Unauthorized:
        return 'invalidCredentials';
      case HttpStatusCode.TooManyRequests:
        return 'tooManyAttempts';
    }
  }

  return 'unknown';
}
