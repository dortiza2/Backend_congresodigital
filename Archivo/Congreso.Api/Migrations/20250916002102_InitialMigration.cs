using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "text", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "text", maxLength: 255, nullable: true),
                    start_time = table.Column<DateTime>(type: "text", nullable: true),
                    end_time = table.Column<DateTime>(type: "text", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    published = table.Column<bool>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "integer", nullable: false),
                    requires_enrollment = table.Column<bool>(type: "integer", nullable: false),
                    activity_type = table.Column<string>(type: "text", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "faq_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    question = table.Column<string>(type: "text", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    published = table.Column<bool>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faq_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "text", maxLength: 100, nullable: true),
                    domain = table.Column<string>(type: "text", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "text", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "speakers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    links = table.Column<string>(type: "text", nullable: true, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speakers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vw_podium_by_year",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    ActivityId = table.Column<Guid>(type: "text", nullable: false),
                    ActivityTitle = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserFullName = table.Column<string>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Prize = table.Column<string>(type: "text", nullable: false),
                    AwardDate = table.Column<DateTime>(type: "text", nullable: false),
                    IsUmg = table.Column<bool>(type: "integer", nullable: false),
                    OrgName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vw_public_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<DateTime>(type: "text", nullable: false),
                    end_time = table.Column<DateTime>(type: "text", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    published = table.Column<bool>(type: "integer", nullable: false),
                    enrolled_count = table.Column<int>(type: "integer", nullable: false),
                    available_spots = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vw_user_enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    UserFullName = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<Guid>(type: "text", nullable: false),
                    ActivityTitle = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsUmg = table.Column<bool>(type: "integer", nullable: false),
                    OrgName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", maxLength: 255, nullable: false),
                    org_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                    table.ForeignKey(
                        name: "FK_teams_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "text", maxLength: 255, nullable: false),
                    org_id = table.Column<int>(type: "integer", nullable: true),
                    org_name = table.Column<string>(type: "text", maxLength: 255, nullable: true),
                    is_umg = table.Column<bool>(type: "integer", nullable: false),
                    avatar_url = table.Column<string>(type: "text", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "text", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "text", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "text", nullable: false),
                    activity_id = table.Column<Guid>(type: "text", nullable: false),
                    seat_number = table.Column<int>(type: "integer", nullable: true),
                    qr_code_id = table.Column<string>(type: "text", maxLength: 100, nullable: true),
                    attended = table.Column<bool>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_enrollments_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_enrollments_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                columns: table => new
                {
                    team_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => new { x.team_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_team_members_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "text", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "winners",
                columns: table => new
                {
                    id = table.Column<long>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    edition_year = table.Column<int>(type: "integer", nullable: false),
                    activity_id = table.Column<Guid>(type: "text", nullable: false),
                    place = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<Guid>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_winners", x => x.id);
                    table.ForeignKey(
                        name: "FK_winners_activities_activity_id",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_winners_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_winners_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_activities_published_start",
                table: "activities",
                columns: new[] { "published", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_activity_id",
                table: "enrollments",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "ux_enrollments_user_activity",
                table: "enrollments",
                columns: new[] { "user_id", "activity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_members_user_id",
                table: "team_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_teams_org_id",
                table: "teams",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_org_id",
                table: "users",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_winners_activity_id",
                table: "winners",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "IX_winners_team_id",
                table: "winners",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_winners_user_id",
                table: "winners",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_winners_year_activity_place",
                table: "winners",
                columns: new[] { "edition_year", "activity_id", "place" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrollments");

            migrationBuilder.DropTable(
                name: "faq_items");

            migrationBuilder.DropTable(
                name: "speakers");

            migrationBuilder.DropTable(
                name: "team_members");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "vw_podium_by_year");

            migrationBuilder.DropTable(
                name: "vw_public_activities");

            migrationBuilder.DropTable(
                name: "vw_user_enrollments");

            migrationBuilder.DropTable(
                name: "winners");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
