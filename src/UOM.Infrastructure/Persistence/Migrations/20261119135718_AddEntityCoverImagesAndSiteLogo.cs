using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityCoverImagesAndSiteLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Venues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogoImageId",
                table: "SiteSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Faculties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Campuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Announcements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_CoverImageId",
                table: "Venues",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_LogoImageId",
                table: "SiteSettings",
                column: "LogoImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_CoverImageId",
                table: "Faculties",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_CoverImageId",
                table: "Campuses",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_CoverImageId",
                table: "Announcements",
                column: "CoverImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_MediaFiles_CoverImageId",
                table: "Announcements",
                column: "CoverImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Campuses_MediaFiles_CoverImageId",
                table: "Campuses",
                column: "CoverImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Faculties_MediaFiles_CoverImageId",
                table: "Faculties",
                column: "CoverImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_MediaFiles_LogoImageId",
                table: "SiteSettings",
                column: "LogoImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_MediaFiles_CoverImageId",
                table: "Venues",
                column: "CoverImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_MediaFiles_CoverImageId",
                table: "Announcements");

            migrationBuilder.DropForeignKey(
                name: "FK_Campuses_MediaFiles_CoverImageId",
                table: "Campuses");

            migrationBuilder.DropForeignKey(
                name: "FK_Faculties_MediaFiles_CoverImageId",
                table: "Faculties");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_MediaFiles_LogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Venues_MediaFiles_CoverImageId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_CoverImageId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_LogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_CoverImageId",
                table: "Faculties");

            migrationBuilder.DropIndex(
                name: "IX_Campuses_CoverImageId",
                table: "Campuses");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_CoverImageId",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "LogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Campuses");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Announcements");
        }
    }
}
