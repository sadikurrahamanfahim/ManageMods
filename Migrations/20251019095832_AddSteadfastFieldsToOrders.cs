using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSteadfastFieldsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "alternative_phone",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_email",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sent_to_steadfast",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sent_to_steadfast_at",
                table: "orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "steadfast_consignment_id",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "steadfast_status",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "steadfast_tracking_code",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "alternative_phone",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "recipient_email",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "sent_to_steadfast",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "sent_to_steadfast_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "steadfast_consignment_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "steadfast_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "steadfast_tracking_code",
                table: "orders");
        }
    }
}
