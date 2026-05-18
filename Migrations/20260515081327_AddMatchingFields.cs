using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetailTambahan",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provinsi",
                table: "Items",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DetailTambahan",
                table: "ItemRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Jumlah",
                table: "ItemRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KondisiMinimum",
                table: "ItemRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Lokasi",
                table: "ItemRequests",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provinsi",
                table: "ItemRequests",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailTambahan",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Provinsi",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DetailTambahan",
                table: "ItemRequests");

            migrationBuilder.DropColumn(
                name: "Jumlah",
                table: "ItemRequests");

            migrationBuilder.DropColumn(
                name: "KondisiMinimum",
                table: "ItemRequests");

            migrationBuilder.DropColumn(
                name: "Lokasi",
                table: "ItemRequests");

            migrationBuilder.DropColumn(
                name: "Provinsi",
                table: "ItemRequests");
        }
    }
}
