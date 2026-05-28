using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTargetRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Items_TargetDonationId",
                table: "Reports");

            migrationBuilder.AddColumn<int>(
                name: "TargetRequestId",
                table: "Reports",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TargetRequestId",
                table: "Reports",
                column: "TargetRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ItemRequests_TargetRequestId",
                table: "Reports",
                column: "TargetRequestId",
                principalTable: "ItemRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Items_TargetDonationId",
                table: "Reports",
                column: "TargetDonationId",
                principalTable: "Items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ItemRequests_TargetRequestId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Items_TargetDonationId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_TargetRequestId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "TargetRequestId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Items_TargetDonationId",
                table: "Reports",
                column: "TargetDonationId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
