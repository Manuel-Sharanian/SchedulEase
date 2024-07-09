using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeautySalon.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToAppointment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Appointments",
                type: "nvarchar(450)",  // Կամ ձեր նախընտրած տիպը
                nullable: false);  // Սյունակը նշված է որպես պարտադիր
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Appointments");
        }
    }
}
