using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsAndRedeemSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RedeemItemId",
                table: "PointTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RedeemItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PointCost = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedeemItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RedeemTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RedeemItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedeemTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedeemTransaction_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RedeemTransaction_RedeemItems_RedeemItemId",
                        column: x => x.RedeemItemId,
                        principalTable: "RedeemItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_RedeemItemId",
                table: "PointTransactions",
                column: "RedeemItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemTransaction_RedeemItemId",
                table: "RedeemTransaction",
                column: "RedeemItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RedeemTransaction_UserId",
                table: "RedeemTransaction",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PointTransactions_RedeemItems_RedeemItemId",
                table: "PointTransactions",
                column: "RedeemItemId",
                principalTable: "RedeemItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointTransactions_RedeemItems_RedeemItemId",
                table: "PointTransactions");

            migrationBuilder.DropTable(
                name: "RedeemTransaction");

            migrationBuilder.DropTable(
                name: "RedeemItems");

            migrationBuilder.DropIndex(
                name: "IX_PointTransactions_RedeemItemId",
                table: "PointTransactions");

            migrationBuilder.DropColumn(
                name: "RedeemItemId",
                table: "PointTransactions");
        }
    }
}
