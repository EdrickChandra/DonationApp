using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddItemImageFileSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "ItemImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "ItemImages");
        }
    }
}
