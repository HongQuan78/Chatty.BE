using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatty.BE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestLogoutToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LatestLogout",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestLogout",
                table: "Users");
        }
    }
}
