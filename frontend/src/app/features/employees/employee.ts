/** Matches EmployeeListItemResponse of the backend. */
export interface EmployeeListItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  /** ISO date (yyyy-MM-dd). */
  entryDate: string;
}
