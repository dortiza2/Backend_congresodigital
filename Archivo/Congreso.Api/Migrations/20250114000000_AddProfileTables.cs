using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create staff_accounts table
            migrationBuilder.CreateTable(
                name: "staff_accounts",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    staff_role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    extra = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    avatar_url = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_accounts", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_staff_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create student_accounts table
            migrationBuilder.CreateTable(
                name: "student_accounts",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    carnet = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    career = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    cohort_year = table.Column<int>(type: "INTEGER", nullable: true),
                    extra = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    avatar_url = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_accounts", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_student_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "idx_staff_accounts_role",
                table: "staff_accounts",
                column: "staff_role");

            migrationBuilder.CreateIndex(
                name: "idx_student_accounts_carnet",
                table: "student_accounts",
                column: "carnet");

            migrationBuilder.CreateIndex(
                name: "idx_student_accounts_career_cohort",
                table: "student_accounts",
                columns: new[] { "career", "cohort_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_accounts");

            migrationBuilder.DropTable(
                name: "student_accounts");
        }
    }
}