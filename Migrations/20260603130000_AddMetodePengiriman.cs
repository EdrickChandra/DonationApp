using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMetodePengiriman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodePengiriman",
                table: "ClaimRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MetodePengiriman",
                table: "RequestOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetodePengiriman",
                table: "ClaimRequests");

            migrationBuilder.DropColumn(
                name: "MetodePengiriman",
                table: "RequestOffers");
        }
    }
}
