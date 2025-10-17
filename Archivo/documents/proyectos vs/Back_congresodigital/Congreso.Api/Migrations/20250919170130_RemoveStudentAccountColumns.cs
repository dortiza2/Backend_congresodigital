using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStudentAccountColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "staff_accounts");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "staff_accounts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "staff_accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                table: "student_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "student_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "student_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                table: "staff_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "staff_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "staff_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
