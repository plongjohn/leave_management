import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { settle } from '../../testing/settle';
import { openConfirmDialog } from './confirm-dialog';

describe('openConfirmDialog', () => {
  const data = {
    title: 'Antrag stornieren?',
    message: 'Urlaub 16.11. – 20.11.2026 wird storniert.',
    confirm: 'Stornieren',
    dismiss: 'Behalten',
  };

  async function open(): Promise<{ result: boolean[]; buttons: HTMLButtonElement[] }> {
    const result: boolean[] = [];
    openConfirmDialog(TestBed.inject(MatDialog), data).subscribe((confirmed) =>
      result.push(confirmed),
    );
    await settle();

    const buttons = [...document.querySelectorAll<HTMLButtonElement>('lm-confirm-dialog button')];
    return { result, buttons };
  }

  // Without animations the dialog closes at once; jsdom never ends a CSS animation.
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    }),
  );

  afterEach(() => TestBed.inject(MatDialog).closeAll());

  it('shows the question with the safe button first', async () => {
    const { buttons } = await open();

    expect(document.querySelector('lm-confirm-dialog h2')?.textContent).toBe('Antrag stornieren?');
    expect(document.querySelector('lm-confirm-dialog mat-dialog-content')?.textContent).toContain(
      'Urlaub 16.11. – 20.11.2026 wird storniert.',
    );
    expect(buttons.map((button) => button.textContent?.trim())).toEqual(['Behalten', 'Stornieren']);
  });

  it.each([
    ['Stornieren', true],
    ['Behalten', false],
  ])('emits the answer when "%s" is clicked', async (label, expected) => {
    const { result, buttons } = await open();

    buttons.find((button) => button.textContent?.trim() === label)!.click();
    await settle();

    expect(result).toEqual([expected]);
  });

  it('counts closing without an answer as "no"', async () => {
    const { result } = await open();

    TestBed.inject(MatDialog).closeAll();
    await settle();

    expect(result).toEqual([false]);
  });
});
