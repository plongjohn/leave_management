import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { EmployeeService } from '../employee.service';
import { employeeTexts } from '../employee.texts';

@Component({
  selector: 'lm-employee-list',
  imports: [DatePipe, MatProgressBarModule, MatTableModule],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeList {
  private readonly employeeService = inject(EmployeeService);

  protected readonly texts = employeeTexts;
  protected readonly displayedColumns = ['name', 'email', 'entryDate'];
  protected readonly employees = rxResource({
    stream: () => this.employeeService.getAll(),
  });
}
