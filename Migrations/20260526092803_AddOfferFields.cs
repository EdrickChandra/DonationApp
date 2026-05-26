using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kondisi",
                table: "RequestOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Lokasi",
                table: "RequestOffers",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NamaBarang",
                table: "RequestOffers",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provinsi",
                table: "RequestOffers",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kondisi",
                table: "RequestOffers");

            migrationBuilder.DropColumn(
                name: "Lokasi",
                table: "RequestOffers");

            migrationBuilder.DropColumn(
                name: "NamaBarang",
                table: "RequestOffers");

            migrationBuilder.DropColumn(
                name: "Provinsi",
                table: "RequestOffers");
        }
    }
}
