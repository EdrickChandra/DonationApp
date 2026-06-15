using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class MoveMetodePengirimanToItemRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodePengiriman",
                table: "ItemRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE ItemRequests
                SET MetodePengiriman = (
                    SELECT ro.MetodePengiriman
                    FROM RequestOffers ro
                    WHERE ro.ItemRequestId = ItemRequests.Id
                      AND ro.Status >= 1
                      AND ro.MetodePengiriman > 0
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1 FROM RequestOffers ro
                    WHERE ro.ItemRequestId = ItemRequests.Id
                      AND ro.Status >= 1
                      AND ro.MetodePengiriman > 0
                );
            ");

            migrationBuilder.DropColumn(
                name: "MetodePengiriman",
                table: "RequestOffers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodePengiriman",
                table: "RequestOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "MetodePengiriman",
                table: "ItemRequests");
        }
    }
}
