import { EmployeeRole } from '../auth/auth.models';

export const layoutTexts = {
  appName: 'Urlaubsverwaltung',
  signOut: 'Abmelden',
  roles: {
    Employee: 'Mitarbeiter',
    Manager: 'Führungskraft',
    Hr: 'Personalabteilung',
  } satisfies Record<EmployeeRole, string>,
} as const;
