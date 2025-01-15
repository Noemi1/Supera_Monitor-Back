using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supera_Monitor_Back.Migrations
{
    /// <inheritdoc />
    public partial class AccountRoleShouldNotBeNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_AccountRole_Role_Id",
                table: "Account");

            migrationBuilder.AlterColumn<int>(
                name: "Role_Id",
                table: "Account",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "PasswordReset", "ResetTokenExpires", "Role_Id", "Verified" },
                values: new object[] { new DateTime(2025, 1, 15, 9, 26, 38, 179, DateTimeKind.Local).AddTicks(5417), null, null, 1, new DateTime(2025, 1, 15, 9, 26, 38, 179, DateTimeKind.Local).AddTicks(5405) });

            migrationBuilder.AddForeignKey(
                name: "FK_Account_AccountRole_Role_Id",
                table: "Account",
                column: "Role_Id",
                principalTable: "AccountRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_AccountRole_Role_Id",
                table: "Account");

            migrationBuilder.AlterColumn<int>(
                name: "Role_Id",
                table: "Account",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "PasswordReset", "ResetTokenExpires", "Role_Id", "Verified" },
                values: new object[] { new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 14, 17, 29, 5, 365, DateTimeKind.Local).AddTicks(1289), new DateTime(2025, 1, 14, 17, 29, 5, 365, DateTimeKind.Local).AddTicks(1276), 8, null });

            migrationBuilder.AddForeignKey(
                name: "FK_Account_AccountRole_Role_Id",
                table: "Account",
                column: "Role_Id",
                principalTable: "AccountRole",
                principalColumn: "Id");
        }
    }
}
