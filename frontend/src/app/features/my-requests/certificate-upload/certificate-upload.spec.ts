import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ValidationError } from '@angular/forms/signals';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { CertificateUpload } from './certificate-upload';

/** Binds the control like a form does, so the two-way bound file can be checked. */
@Component({
  imports: [CertificateUpload],
  template: `
    <lm-certificate-upload
      [(value)]="file"
      [required]="required()"
      [touched]="touched()"
      [errors]="errors()"
    />
  `,
})
class Host {
  readonly file = signal<File | null>(null);
  readonly required = signal(false);
  readonly touched = signal(false);
  readonly errors = signal<readonly ValidationError[]>([]);
}

describe('CertificateUpload', () => {
  const pdf = new File(['x'.repeat(240 * 1024)], 'attest.pdf', { type: 'application/pdf' });

  async function render(
    setup: (host: Host) => void = () => undefined,
  ): Promise<{ host: Host; element: HTMLElement; refresh: () => Promise<void> }> {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    const fixture = TestBed.createComponent(Host);
    setup(fixture.componentInstance);
    await fixture.whenStable();
    return {
      host: fixture.componentInstance,
      element: fixture.nativeElement as HTMLElement,
      refresh: () => fixture.whenStable(),
    };
  }

  function text(element: Element | null): string {
    return (element?.textContent ?? '').replace(/\s+/g, ' ').trim();
  }

  it('takes the file chosen in the file dialog', async () => {
    const { host, element, refresh } = await render();
    const input = element.querySelector<HTMLInputElement>('input[type="file"]')!;

    expect(input.accept).toBe('application/pdf,image/jpeg,image/png');

    Object.defineProperty(input, 'files', { value: [pdf] });
    input.dispatchEvent(new Event('change'));
    await refresh();

    expect(host.file()).toBe(pdf);
    expect(text(element.querySelector('.file-name'))).toBe('attest.pdf');
    expect(text(element.querySelector('.file-size'))).toBe('240 KB');
  });

  it('takes a dropped file', async () => {
    const { host, element, refresh } = await render();

    const drop = new Event('drop', { cancelable: true });
    Object.defineProperty(drop, 'dataTransfer', { value: { files: [pdf] } });
    element.querySelector('.dropzone')!.dispatchEvent(drop);
    await refresh();

    expect(host.file()).toBe(pdf);
    expect(drop.defaultPrevented).toBe(true);
  });

  it('removes the file again', async () => {
    const { host, element, refresh } = await render((host) => host.file.set(pdf));

    element.querySelector<HTMLButtonElement>('button[aria-label="attest.pdf entfernen"]')!.click();
    await refresh();

    expect(host.file()).toBeNull();
    expect(element.querySelector('.dropzone')).not.toBeNull();
  });

  it('marks the field as optional or required', async () => {
    const { host, element, refresh } = await render();
    expect(text(element.querySelector('.label'))).toBe('Ärztliche Bestätigung (optional)');

    host.required.set(true);
    await refresh();

    expect(text(element.querySelector('.label'))).toBe('Ärztliche Bestätigung *');
  });

  it('shows a missing file only after a submit attempt, a wrong file at once', async () => {
    const missing = [{ kind: 'required', message: 'Bitte lade die ärztliche Bestätigung hoch.' }];
    const { host, element, refresh } = await render((host) => host.errors.set(missing));
    expect(element.querySelector('[role="alert"]')).toBeNull();

    host.touched.set(true);
    await refresh();
    expect(text(element.querySelector('[role="alert"]'))).toBe(
      'Bitte lade die ärztliche Bestätigung hoch.',
    );

    host.touched.set(false);
    host.file.set(new File(['x'], 'foto.gif', { type: 'image/gif' }));
    host.errors.set([
      { kind: 'certificate_type', message: 'Bitte wähle eine PDF-, JPG- oder PNG-Datei.' },
    ]);
    await refresh();
    expect(text(element.querySelector('[role="alert"]'))).toBe(
      'Bitte wähle eine PDF-, JPG- oder PNG-Datei.',
    );
  });
});
