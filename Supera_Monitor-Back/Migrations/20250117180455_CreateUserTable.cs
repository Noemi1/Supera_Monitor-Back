using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supera_Monitor_Back.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "User_Id",
                table: "Account",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Account_Created_Id = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deactivated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Account_CreatedId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Account_Account_CreatedId",
                        column: x => x.Account_CreatedId,
                        principalTable: "Account",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "User_Id", "Verified" },
                values: new object[] { new DateTime(2025, 1, 17, 15, 4, 55, 307, DateTimeKind.Local).AddTicks(7083), null, new DateTime(2025, 1, 17, 15, 4, 55, 307, DateTimeKind.Local).AddTicks(7071) });

            migrationBuilder.CreateIndex(
                name: "IX_Account_User_Id",
                table: "Account",
                column: "User_Id");

            migrationBuilder.CreateIndex(
                name: "IX_User_Account_CreatedId",
                table: "User",
                column: "Account_CreatedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Account_User_User_Id",
                table: "Account",
                column: "User_Id",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_User_User_Id",
                table: "Account");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropIndex(
                name: "IX_Account_User_Id",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "User_Id",
                table: "Account");

            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Verified" },
                values: new object[] { new DateTime(2025, 1, 16, 14, 49, 32, 43, DateTimeKind.Local).AddTicks(4090), new DateTime(2025, 1, 16, 14, 49, 32, 43, DateTimeKind.Local).AddTicks(4076) });
        }
    }
}
