using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class AutoFixSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_organizations_org_id",
                table: "teams");

            migrationBuilder.DropIndex(
                name: "IX_teams_org_id",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "extra",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "links",
                table: "speakers");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "speakers",
                newName: "photo_url");

            migrationBuilder.RenameColumn(
                name: "avatar_url",
                table: "speakers",
                newName: "org_name");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "student_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "enabled_at",
                table: "student_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "student_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "organization",
                table: "student_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "student_accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                table: "speakers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "speakers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "speakers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "social",
                table: "speakers",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "qr_jwt_ids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jwt_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    used_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_jwt_ids", x => x.id);
                    table.ForeignKey(
                        name: "FK_qr_jwt_ids_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_qr_jwt_ids_users_used_by",
                        column: x => x.used_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_qr_jwt_ids_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teams_OrganizationId",
                table: "teams",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "idx_qr_jwt_ids_expires_at",
                table: "qr_jwt_ids",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_qr_jwt_ids_user_activity",
                table: "qr_jwt_ids",
                columns: new[] { "user_id", "activity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qr_jwt_ids_activity_id",
                table: "qr_jwt_ids",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "IX_qr_jwt_ids_used_by",
                table: "qr_jwt_ids",
                column: "used_by");

            migrationBuilder.CreateIndex(
                name: "ux_qr_jwt_ids_jwt_id",
                table: "qr_jwt_ids",
                column: "jwt_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_teams_organizations_OrganizationId",
                table: "teams",
                column: "OrganizationId",
                principalTable: "organizations",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_teams_organizations_OrganizationId",
                table: "teams");

            migrationBuilder.DropTable(
                name: "qr_jwt_ids");

            migrationBuilder.DropIndex(
                name: "IX_teams_OrganizationId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_login",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "enabled_at",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "organization",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "student_accounts");

            migrationBuilder.DropColumn(
                name: "contact_email",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "social",
                table: "speakers");

            migrationBuilder.RenameColumn(
                name: "photo_url",
                table: "speakers",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "org_name",
                table: "speakers",
                newName: "avatar_url");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "extra",
                table: "student_accounts",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "links",
                table: "speakers",
                type: "jsonb",
                nullable: true,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_teams_org_id",
                table: "teams",
                column: "org_id");

            migrationBuilder.AddForeignKey(
                name: "FK_teams_organizations_org_id",
                table: "teams",
                column: "org_id",
                principalTable: "organizations",
                principalColumn: "id");
        }
    }
}
