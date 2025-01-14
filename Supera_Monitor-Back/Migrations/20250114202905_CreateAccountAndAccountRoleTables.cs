using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Supera_Monitor_Back.Migrations
{
    /// <inheritdoc />
    public partial class CreateAccountAndAccountRoleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptTerms = table.Column<bool>(type: "bit", nullable: false),
                    VerificationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Verified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResetTokenExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordReset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Role_Id = table.Column<int>(type: "int", nullable: true),
                    Account_Created_Id = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deactivated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_AccountRole_Role_Id",
                        column: x => x.Role_Id,
                        principalTable: "AccountRole",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Account_Account_Account_Created_Id",
                        column: x => x.Account_Created_Id,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountRefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Account_Id = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Revoked = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRefreshToken_Account_Account_Id",
                        column: x => x.Account_Id,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccountRole",
                columns: new[] { "Id", "Role" },
                values: new object[,]
                {
                    { 1, "Student" },
                    { 2, "Assistant" },
                    { 4, "Teacher" },
                    { 8, "Admin" }
                });

            migrationBuilder.InsertData(
                table: "Account",
                columns: new[] { "Id", "AcceptTerms", "Account_Created_Id", "Created", "Deactivated", "Email", "LastUpdated", "Name", "PasswordHash", "PasswordReset", "Phone", "ResetToken", "ResetTokenExpires", "Role_Id", "VerificationToken", "Verified" },
                values: new object[] { 1, true, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "galax1y@test.com", null, "galax1y", "$2b$10$a46QGCAIbzhXEKJl36cD1OBQE5xMNyATdvrrfh1s/wtqTdawg2lHu", new DateTime(2025, 1, 14, 17, 29, 5, 365, DateTimeKind.Local).AddTicks(1289), "123456789", null, new DateTime(2025, 1, 14, 17, 29, 5, 365, DateTimeKind.Local).AddTicks(1276), 8, "", null });

            migrationBuilder.CreateIndex(
                name: "IX_Account_Account_Created_Id",
                table: "Account",
                column: "Account_Created_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Role_Id",
                table: "Account",
                column: "Role_Id");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRefreshToken_Account_Id",
                table: "AccountRefreshToken",
                column: "Account_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRefreshToken");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "AccountRole");
        }
    }
}
