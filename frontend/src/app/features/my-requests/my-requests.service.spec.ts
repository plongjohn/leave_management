import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { MyRequestsService } from './my-requests.service';

describe('MyRequestsService', () => {
  let service: MyRequestsService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(MyRequestsService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads the requests of one year', () => {
    void firstValueFrom(service.getRequests(2026));

    const request = httpTesting.expectOne('/api/me/leave-requests?year=2026');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('sends the draft for the preview', () => {
    const draft = { startDate: '2026-11-16', endDate: '2026-11-20', type: 'Vacation' } as const;
    void firstValueFrom(service.preview(draft));

    const request = httpTesting.expectOne('/api/me/leave-requests/preview');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(draft);
    request.flush({});
  });

  it('reports sick leave with its certificate as multipart form', () => {
    const certificate = new File(['%PDF-1.7'], 'attest.pdf', { type: 'application/pdf' });
    void firstValueFrom(
      service.reportSickLeave({ startDate: '2026-11-02', endDate: '2026-11-06', certificate }),
    );

    const request = httpTesting.expectOne('/api/me/sick-leaves');
    const form = request.request.body as FormData;
    expect(request.request.method).toBe('POST');
    expect(form.get('startDate')).toBe('2026-11-02');
    expect(form.get('endDate')).toBe('2026-11-06');
    expect(form.has('comment')).toBe(false);
    expect((form.get('certificate') as File).name).toBe('attest.pdf');
    request.flush({});
  });

  it('reports sick leave without a certificate', () => {
    void firstValueFrom(
      service.reportSickLeave({
        startDate: '2026-11-02',
        endDate: '2026-11-03',
        comment: 'Grippe',
      }),
    );

    const request = httpTesting.expectOne('/api/me/sick-leaves');
    const form = request.request.body as FormData;
    expect(form.get('comment')).toBe('Grippe');
    expect(form.has('certificate')).toBe(false);
    request.flush({});
  });

  it('cancels with the version the user saw', () => {
    void firstValueFrom(service.cancel({ id: 'r1', version: 4711 }));

    const request = httpTesting.expectOne('/api/me/leave-requests/r1/cancel');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ version: 4711 });
    request.flush(null, { status: 204, statusText: 'No Content' });
  });
});
