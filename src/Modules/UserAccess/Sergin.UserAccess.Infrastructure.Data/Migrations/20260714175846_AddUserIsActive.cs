using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sergin.UserAccess.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddUserIsActive : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            schema: "ua",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_active",
            schema: "ua",
            table: "users");
    }
}
