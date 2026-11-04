import { EmployeeRole } from '../auth/auth.models';

export const layoutTexts = {
  appName: 'Urlaubsverwaltung',
  signOut: 'Abmelden',
  mainNavigation: 'Hauptnavigation',
  toggleNavigation: 'Navigation ein- oder ausblenden',
  nav: {
    overview: 'Übersicht',
    employees: 'Mitarbeiter',
  },
  roles: {
    Employee: 'Mitarbeiter',
    Manager: 'Führungskraft',
    Hr: 'Personalabteilung',
  } satisfies Record<EmployeeRole, string>,
} as const;
