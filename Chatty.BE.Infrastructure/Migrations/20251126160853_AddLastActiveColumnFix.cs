using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatty.BE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastActiveColumnFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: LastActive is already added by 20251126160332_AddLastActiveToUsers.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op to keep rollback chain consistent with Up().
        }
    }
}
