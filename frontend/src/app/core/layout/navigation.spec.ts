import { NavItem, navItemsFor } from './navigation';

describe('navItemsFor', () => {
  const items: readonly NavItem[] = [
    { path: '/overview', label: 'Übersicht', icon: 'home' },
    { path: '/approvals', label: 'Genehmigungen', icon: 'task_alt', roles: ['Manager', 'Hr'] },
    { path: '/admin', label: 'Verwaltung', icon: 'settings', roles: ['Hr'] },
  ];

  it.each([
    ['Employee', ['/overview']],
    ['Manager', ['/overview', '/approvals']],
    ['Hr', ['/overview', '/approvals', '/admin']],
  ] as const)('shows %s only the entries for that role', (role, expected) => {
    expect(navItemsFor(role, items).map((item) => item.path)).toEqual(expected);
  });
});
