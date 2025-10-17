using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelForTesting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:staff_role", "admin_dev,admin,asistente");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "extra",
                table: "student_accounts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "extra",
                table: "staff_accounts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<int>(
                name: "level",
                table: "roles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "checkin_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkin_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_checkin_tokens_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_checkin_tokens_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_checkin_tokens_users_used_by",
                        column: x => x.used_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_checkin_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkin_tokens_activity_id",
                table: "checkin_tokens",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkin_tokens_enrollment_id",
                table: "checkin_tokens",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkin_tokens_used_by",
                table: "checkin_tokens",
                column: "used_by");

            migrationBuilder.CreateIndex(
                name: "IX_checkin_tokens_user_id",
                table: "checkin_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkin_tokens");

            migrationBuilder.DropColumn(
                name: "level",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "name",
                table: "roles");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:staff_role", "admin_dev,admin,asistente");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "extra",
                table: "student_accounts",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "extra",
                table: "staff_accounts",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'::jsonb");
        }
    }
}
