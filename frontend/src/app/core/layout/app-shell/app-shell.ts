import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { layoutTexts } from '../layout.texts';

/**
 * Frame of the signed-in app. Kept minimal until the navigation and layout get their own
 * design input (phase 6).
 */
@Component({
  selector: 'lm-app-shell',
  imports: [MatButtonModule, MatIconModule, MatToolbarModule, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  private readonly router = inject(Router);

  protected readonly auth = inject(AuthService);
  protected readonly texts = layoutTexts;

  protected signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}
