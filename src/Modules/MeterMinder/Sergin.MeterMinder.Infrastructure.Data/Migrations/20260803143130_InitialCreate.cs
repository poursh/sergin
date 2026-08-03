using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sergin.MeterMinder.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "mm");

        migrationBuilder.CreateTable(
            name: "manufacturer",
            schema: "mm",
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

        migrationBuilder.CreateTable(
            name: "device",
            schema: "mm",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                device_id = table.Column<string>(type: "text", nullable: false),
                manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_device", x => x.id);
                table.ForeignKey(
                    name: "fk_device_manufacturer_manufacturer_id",
                    column: x => x.manufacturer_id,
                    principalSchema: "mm",
                    principalTable: "manufacturer",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_device_manufacturer_id",
            schema: "mm",
            table: "device",
            column: "manufacturer_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "device",
            schema: "mm");

        migrationBuilder.DropTable(
            name: "manufacturer",
            schema: "mm");
    }
}
