import { Component, computed, input, model, signal } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { APP_LOCALE } from '../../../core/i18n/locale.providers';
import { certificateTexts } from '../my-requests.texts';

/** What the backend accepts (ADR 0008); it checks the file's first bytes again. */
export const CERTIFICATE_TYPES = ['application/pdf', 'image/jpeg', 'image/png'];
export const CERTIFICATE_MAX_BYTES = 5 * 1024 * 1024;

const sizeFormat = new Intl.NumberFormat(APP_LOCALE, { maximumFractionDigits: 1 });

/**
 * Picks the medical certificate by drag and drop or file dialog. A Signal Forms control: bound with
 * `[formField]`, the form keeps the file and its rules (required, type, size) in its schema.
 */
@Component({
  selector: 'lm-certificate-upload',
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './certificate-upload.html',
  styleUrl: './certificate-upload.scss',
})
export class CertificateUpload implements FormValueControl<File | null> {
  readonly value = model<File | null>(null);
  readonly required = input(false);
  readonly touched = input(false);
  readonly errors = input<readonly ValidationError[]>([]);

  protected readonly texts = certificateTexts;
  protected readonly accept = CERTIFICATE_TYPES.join(',');
  protected readonly dragging = signal(false);

  // A wrong file is reported at once; a missing one only after a submit attempt marks it touched.
  protected readonly error = computed(() =>
    this.touched() || this.value() ? this.errors()[0]?.message : undefined,
  );

  protected readonly size = computed(() => {
    const bytes = this.value()?.size ?? 0;
    return bytes < 1024 * 1024
      ? `${sizeFormat.format(Math.max(1, Math.round(bytes / 1024)))} KB`
      : `${sizeFormat.format(bytes / (1024 * 1024))} MB`;
  });

  protected choose(input: HTMLInputElement): void {
    this.value.set(input.files?.[0] ?? null);
    // Allows choosing the same file again after removing it.
    input.value = '';
  }

  protected drop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.value.set(file);
    }
  }

  protected dragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }
}
