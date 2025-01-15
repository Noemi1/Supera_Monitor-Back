using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Supera_Monitor_Back.Migrations
{
    /// <inheritdoc />
    public partial class AccountListView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Verified" },
                values: new object[] { new DateTime(2025, 1, 15, 11, 7, 42, 687, DateTimeKind.Local).AddTicks(2018), new DateTime(2025, 1, 15, 11, 7, 42, 687, DateTimeKind.Local).AddTicks(2007) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Account",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Verified" },
                values: new object[] { new DateTime(2025, 1, 15, 9, 26, 38, 179, DateTimeKind.Local).AddTicks(5417), new DateTime(2025, 1, 15, 9, 26, 38, 179, DateTimeKind.Local).AddTicks(5405) });
        }
    }
}
