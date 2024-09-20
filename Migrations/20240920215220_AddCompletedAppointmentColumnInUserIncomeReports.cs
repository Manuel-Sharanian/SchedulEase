using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautySalon.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedAppointmentColumnInUserIncomeReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedAppointments",
                table: "UserIncomeReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAppointments",
                table: "UserIncomeReports");
        }
    }
}
