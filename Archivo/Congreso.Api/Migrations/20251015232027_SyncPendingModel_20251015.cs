using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Congreso.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModel_20251015 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "id_guid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id_guid",
                table: "users",
                newName: "id");
        }
    }
}
