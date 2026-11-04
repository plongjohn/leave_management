using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "employees",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                // Existing employees become regular employees; managers and HR are assigned afterwards.
                defaultValue: "Employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "employees");
        }
    }
}
