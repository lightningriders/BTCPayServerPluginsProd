using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Lqwd.Migrations
{
    /// <inheritdoc />
    public partial class Database_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "Settings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreId",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "Settings");
        }
    }
}
