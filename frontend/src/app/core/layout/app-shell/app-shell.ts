import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, computed, inject, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { layoutTexts } from '../layout.texts';
import { navItemsFor } from '../navigation';

/** Below this width the navigation slides in over the content instead of sitting beside it. */
export const COMPACT_LAYOUT_QUERY = '(max-width: 959.98px)';

/** Frame of the signed-in app: toolbar on top, side navigation, page content. */
@Component({
  selector: 'lm-app-shell',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {
  private readonly router = inject(Router);
  private readonly sidenav = viewChild.required(MatSidenav);

  protected readonly auth = inject(AuthService);
  protected readonly texts = layoutTexts;

  protected readonly compact = toSignal(
    inject(BreakpointObserver)
      .observe(COMPACT_LAYOUT_QUERY)
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly navItems = computed(() => {
    const user = this.auth.currentUser();
    return user ? navItemsFor(user.role) : [];
  });

  /** On small screens the overlay would cover the page that was just opened. */
  protected onNavigate(): void {
    if (this.compact()) {
      void this.sidenav().close();
    }
  }

  protected signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}
