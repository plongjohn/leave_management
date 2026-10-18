import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeListItem } from './employee';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<EmployeeListItem[]> {
    return this.http.get<EmployeeListItem[]>('/api/employees');
  }
}
