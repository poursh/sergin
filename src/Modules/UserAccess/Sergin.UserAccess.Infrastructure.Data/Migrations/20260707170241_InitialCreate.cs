using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sergin.UserAccess.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "ua");

        migrationBuilder.CreateTable(
            name: "users",
            schema: "ua",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "users",
            schema: "ua");
    }
}
