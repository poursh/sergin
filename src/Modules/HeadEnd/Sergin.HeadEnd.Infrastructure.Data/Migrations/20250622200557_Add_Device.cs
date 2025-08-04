using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sergin.HeadEnd.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Add_Device : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "hes");

        migrationBuilder.CreateTable(
            name: "device",
            schema: "hes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                device_id = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_device", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "device",
            schema: "hes");
    }
}
