using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LeaveManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "holidays",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holidays", x => x.date);
                });

            migrationBuilder.CreateTable(
                name: "leave_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_entitlements_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    special_leave_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    working_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_requests_employees_decided_by_id",
                        column: x => x.decided_by_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "leave_request_days",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    working_days = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_request_days", x => new { x.leave_request_id, x.date });
                    table.ForeignKey(
                        name: "fk_leave_request_days_leave_requests_leave_request_id",
                        column: x => x.leave_request_id,
                        principalTable: "leave_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "holidays",
                columns: new[] { "date", "kind", "name" },
                values: new object[,]
                {
                    { new DateOnly(2025, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2025, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2025, 4, 21), "Statutory", "Ostermontag" },
                    { new DateOnly(2025, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2025, 5, 29), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2025, 6, 9), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2025, 6, 19), "Statutory", "Fronleichnam" },
                    { new DateOnly(2025, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2025, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2025, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2025, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2025, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2025, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2025, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2025, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2026, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2026, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2026, 4, 6), "Statutory", "Ostermontag" },
                    { new DateOnly(2026, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2026, 5, 14), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2026, 5, 25), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2026, 6, 4), "Statutory", "Fronleichnam" },
                    { new DateOnly(2026, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2026, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2026, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2026, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2026, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2026, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2026, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2026, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2027, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2027, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2027, 3, 29), "Statutory", "Ostermontag" },
                    { new DateOnly(2027, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2027, 5, 6), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2027, 5, 17), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2027, 5, 27), "Statutory", "Fronleichnam" },
                    { new DateOnly(2027, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2027, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2027, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2027, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2027, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2027, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2027, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2027, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2028, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2028, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2028, 4, 17), "Statutory", "Ostermontag" },
                    { new DateOnly(2028, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2028, 5, 25), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2028, 6, 5), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2028, 6, 15), "Statutory", "Fronleichnam" },
                    { new DateOnly(2028, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2028, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2028, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2028, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2028, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2028, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2028, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2028, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2029, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2029, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2029, 4, 2), "Statutory", "Ostermontag" },
                    { new DateOnly(2029, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2029, 5, 10), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2029, 5, 21), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2029, 5, 31), "Statutory", "Fronleichnam" },
                    { new DateOnly(2029, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2029, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2029, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2029, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2029, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2029, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2029, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2029, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2030, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2030, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2030, 4, 22), "Statutory", "Ostermontag" },
                    { new DateOnly(2030, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2030, 5, 30), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2030, 6, 10), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2030, 6, 20), "Statutory", "Fronleichnam" },
                    { new DateOnly(2030, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2030, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2030, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2030, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2030, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2030, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2030, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2030, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2031, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2031, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2031, 4, 14), "Statutory", "Ostermontag" },
                    { new DateOnly(2031, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2031, 5, 22), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2031, 6, 2), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2031, 6, 12), "Statutory", "Fronleichnam" },
                    { new DateOnly(2031, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2031, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2031, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2031, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2031, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2031, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2031, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2031, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2032, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2032, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2032, 3, 29), "Statutory", "Ostermontag" },
                    { new DateOnly(2032, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2032, 5, 6), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2032, 5, 17), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2032, 5, 27), "Statutory", "Fronleichnam" },
                    { new DateOnly(2032, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2032, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2032, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2032, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2032, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2032, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2032, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2032, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2033, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2033, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2033, 4, 18), "Statutory", "Ostermontag" },
                    { new DateOnly(2033, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2033, 5, 26), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2033, 6, 6), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2033, 6, 16), "Statutory", "Fronleichnam" },
                    { new DateOnly(2033, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2033, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2033, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2033, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2033, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2033, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2033, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2033, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2034, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2034, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2034, 4, 10), "Statutory", "Ostermontag" },
                    { new DateOnly(2034, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2034, 5, 18), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2034, 5, 29), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2034, 6, 8), "Statutory", "Fronleichnam" },
                    { new DateOnly(2034, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2034, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2034, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2034, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2034, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2034, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2034, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2034, 12, 31), "CompanyHalfDay", "Silvester" },
                    { new DateOnly(2035, 1, 1), "Statutory", "Neujahr" },
                    { new DateOnly(2035, 1, 6), "Statutory", "Heilige Drei Könige" },
                    { new DateOnly(2035, 3, 26), "Statutory", "Ostermontag" },
                    { new DateOnly(2035, 5, 1), "Statutory", "Staatsfeiertag" },
                    { new DateOnly(2035, 5, 3), "Statutory", "Christi Himmelfahrt" },
                    { new DateOnly(2035, 5, 14), "Statutory", "Pfingstmontag" },
                    { new DateOnly(2035, 5, 24), "Statutory", "Fronleichnam" },
                    { new DateOnly(2035, 8, 15), "Statutory", "Mariä Himmelfahrt" },
                    { new DateOnly(2035, 10, 26), "Statutory", "Nationalfeiertag" },
                    { new DateOnly(2035, 11, 1), "Statutory", "Allerheiligen" },
                    { new DateOnly(2035, 12, 8), "Statutory", "Mariä Empfängnis" },
                    { new DateOnly(2035, 12, 24), "CompanyHalfDay", "Heiliger Abend" },
                    { new DateOnly(2035, 12, 25), "Statutory", "Christtag" },
                    { new DateOnly(2035, 12, 26), "Statutory", "Stefanitag" },
                    { new DateOnly(2035, 12, 31), "CompanyHalfDay", "Silvester" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_leave_entitlements_employee_id_year",
                table: "leave_entitlements",
                columns: new[] { "employee_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leave_request_days_date",
                table: "leave_request_days",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_decided_by_id",
                table: "leave_requests",
                column: "decided_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_employee_id_start_date",
                table: "leave_requests",
                columns: new[] { "employee_id", "start_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holidays");

            migrationBuilder.DropTable(
                name: "leave_entitlements");

            migrationBuilder.DropTable(
                name: "leave_request_days");

            migrationBuilder.DropTable(
                name: "leave_requests");
        }
    }
}
