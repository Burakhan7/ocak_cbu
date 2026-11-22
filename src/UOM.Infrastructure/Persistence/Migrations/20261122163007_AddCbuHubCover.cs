using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCbuHubCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CbuHubCoverImageId",
                table: "SiteSettings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_CbuHubCoverImageId",
                table: "SiteSettings",
                column: "CbuHubCoverImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_MediaFiles_CbuHubCoverImageId",
                table: "SiteSettings",
                column: "CbuHubCoverImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_MediaFiles_CbuHubCoverImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_CbuHubCoverImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CbuHubCoverImageId",
                table: "SiteSettings");
        }
    }
}
