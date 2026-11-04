import { TestBed } from '@angular/core/testing';
import { LeaveRequestStatus, LeaveType } from '../leave.models';
import { LeaveStatusChip } from './leave-status-chip';

describe('LeaveStatusChip', () => {
  async function render(status: LeaveRequestStatus, type: LeaveType): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(LeaveStatusChip);
    fixture.componentRef.setInput('status', status);
    fixture.componentRef.setInput('type', type);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it.each([
    ['Pending', 'Offen', 'pending'],
    ['Approved', 'Genehmigt', 'approved'],
    ['Rejected', 'Abgelehnt', 'rejected'],
    ['Cancelled', 'Storniert', 'cancelled'],
  ] as const)('shows %s vacation as "%s"', async (status, label, variant) => {
    const element = await render(status, 'Vacation');

    expect(element.textContent).toBe(label);
    expect(element.classList).toContain(variant);
  });

  it('shows approved sick leave as reported', async () => {
    const element = await render('Approved', 'SickLeave');

    expect(element.textContent).toBe('Gemeldet');
    expect(element.classList).toContain('reported');
  });

  it('shows cancelled sick leave as cancelled', async () => {
    const element = await render('Cancelled', 'SickLeave');

    expect(element.textContent).toBe('Storniert');
  });
});
