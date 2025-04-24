using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BTCPayServer.Plugins.Lqwd.Migrations
{
    /// <inheritdoc />
    public partial class Database_v0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.CreateTable(
                name: "Bolt11PaymentData",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    State = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeeTotalSat = table.Column<long>(type: "bigint", nullable: false),
                    OrderTotalSat = table.Column<long>(type: "bigint", nullable: false),
                    Invoice = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolt11PaymentData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelData",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FundedAt = table.Column<string>(type: "text", nullable: true),
                    FundingOutpoint = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnchainPaymentData",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    State = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeeTotalSat = table.Column<long>(type: "bigint", nullable: false),
                    OrderTotalSat = table.Column<long>(type: "bigint", nullable: false),
                    OnchainAddress = table.Column<string>(type: "text", nullable: true),
                    MinOnchainPaymentConfirmations = table.Column<int>(type: "integer", nullable: false),
                    MinFeeFor0Conf = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnchainPaymentData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PluginRecords",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentData",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<string>(type: "text", nullable: true),
                    Bolt11Id = table.Column<int>(type: "integer", nullable: true),
                    OnchainId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentData_Bolt11PaymentData_Bolt11Id",
                        column: x => x.Bolt11Id,
                        principalSchema: "BTCPayServer.Plugins.Lqwd",
                        principalTable: "Bolt11PaymentData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentData_OnchainPaymentData_OnchainId",
                        column: x => x.OnchainId,
                        principalSchema: "BTCPayServer.Plugins.Lqwd",
                        principalTable: "OnchainPaymentData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "BTCPayServer.Plugins.Lqwd",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "text", nullable: false),
                    StoreId = table.Column<string>(type: "text", nullable: true),
                    LspBalanceSat = table.Column<long>(type: "bigint", nullable: false),
                    ClientBalanceSat = table.Column<long>(type: "bigint", nullable: false),
                    RequiredChannelConfirmations = table.Column<int>(type: "integer", nullable: false),
                    FundingConfirmsWithinBlocks = table.Column<int>(type: "integer", nullable: false),
                    ChannelExpiryBlocks = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: true),
                    RefundOnchainAddress = table.Column<string>(type: "text", nullable: true),
                    AnnounceChannel = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OrderState = table.Column<string>(type: "text", nullable: true),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    ChannelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_ChannelData_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "BTCPayServer.Plugins.Lqwd",
                        principalTable: "ChannelData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_PaymentData_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "BTCPayServer.Plugins.Lqwd",
                        principalTable: "PaymentData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ChannelId",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "Orders",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentId",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "Orders",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentData_Bolt11Id",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "PaymentData",
                column: "Bolt11Id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentData_OnchainId",
                schema: "BTCPayServer.Plugins.Lqwd",
                table: "PaymentData",
                column: "OnchainId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders",
                schema: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.DropTable(
                name: "PluginRecords",
                schema: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.DropTable(
                name: "ChannelData",
                schema: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.DropTable(
                name: "PaymentData",
                schema: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.DropTable(
                name: "Bolt11PaymentData",
                schema: "BTCPayServer.Plugins.Lqwd");

            migrationBuilder.DropTable(
                name: "OnchainPaymentData",
                schema: "BTCPayServer.Plugins.Lqwd");
        }
    }
}
