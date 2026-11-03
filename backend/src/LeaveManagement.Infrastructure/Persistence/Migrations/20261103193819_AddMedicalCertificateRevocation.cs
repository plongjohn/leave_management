using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCertificateRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "certificate_revocation_reason",
                table: "leave_requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "certificate_revoked_at",
                table: "leave_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "certificate_revoked_by_id",
                table: "leave_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_certificate_revoked_by_id",
                table: "leave_requests",
                column: "certificate_revoked_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_leave_requests_employees_certificate_revoked_by_id",
                table: "leave_requests",
                column: "certificate_revoked_by_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_leave_requests_employees_certificate_revoked_by_id",
                table: "leave_requests");

            migrationBuilder.DropIndex(
                name: "ix_leave_requests_certificate_revoked_by_id",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "certificate_revocation_reason",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "certificate_revoked_at",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "certificate_revoked_by_id",
                table: "leave_requests");
        }
    }
}
