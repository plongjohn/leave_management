import { EmployeeRole } from '../auth/auth.models';
import { layoutTexts } from './layout.texts';

/** One entry of the side navigation. */
export interface NavItem {
  readonly path: string;
  readonly label: string;
  /** Material Symbols ligature. */
  readonly icon: string;
  /** Roles that see the entry; without roles everyone does. Hides links only – the API decides. */
  readonly roles?: readonly EmployeeRole[];
}

/** Entries in display order. Each feature adds its entry together with its route. */
export const navItems: readonly NavItem[] = [
  { path: '/overview', label: layoutTexts.nav.overview, icon: 'home' },
  { path: '/employees', label: layoutTexts.nav.employees, icon: 'group' },
];

export function navItemsFor(
  role: EmployeeRole,
  items: readonly NavItem[] = navItems,
): readonly NavItem[] {
  return items.filter((item) => !item.roles || item.roles.includes(role));
}
