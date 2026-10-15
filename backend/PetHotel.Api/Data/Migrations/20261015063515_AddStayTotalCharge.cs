using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetHotel.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStayTotalCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCharge",
                table: "Stays",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCharge",
                table: "Stays");
        }
    }
}
