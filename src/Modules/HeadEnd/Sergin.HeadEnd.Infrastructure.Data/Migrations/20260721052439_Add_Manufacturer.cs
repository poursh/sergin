using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sergin.HeadEnd.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Add_Manufacturer : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "manufacturer_id",
            schema: "hes",
            table: "device",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.CreateTable(
            name: "manufacturer",
            schema: "hes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                address = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_manufacturer", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_device_manufacturer_id",
            schema: "hes",
            table: "device",
            column: "manufacturer_id");

        migrationBuilder.AddForeignKey(
            name: "fk_device_manufacturer_manufacturer_id",
            schema: "hes",
            table: "device",
            column: "manufacturer_id",
            principalSchema: "hes",
            principalTable: "manufacturer",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_device_manufacturer_manufacturer_id",
            schema: "hes",
            table: "device");

        migrationBuilder.DropTable(
            name: "manufacturer",
            schema: "hes");

        migrationBuilder.DropIndex(
            name: "ix_device_manufacturer_id",
            schema: "hes",
            table: "device");

        migrationBuilder.DropColumn(
            name: "manufacturer_id",
            schema: "hes",
            table: "device");
    }
}
