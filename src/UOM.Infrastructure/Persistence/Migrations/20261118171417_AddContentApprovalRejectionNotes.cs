using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentApprovalRejectionNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRejectionNote",
                table: "NewsPosts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastRejectionNote",
                table: "Events",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRejectionNote",
                table: "NewsPosts");

            migrationBuilder.DropColumn(
                name: "LastRejectionNote",
                table: "Events");
        }
    }
}
