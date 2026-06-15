using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class RenameTrustScoreToAvgRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrustScore",
                table: "AspNetUsers",
                newName: "AvgRating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvgRating",
                table: "AspNetUsers",
                newName: "TrustScore");
        }
    }
}
