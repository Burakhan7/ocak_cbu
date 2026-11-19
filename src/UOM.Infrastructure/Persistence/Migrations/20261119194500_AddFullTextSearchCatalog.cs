using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UOM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            FullTextSearchSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            FullTextSearchSql.Remove(migrationBuilder);
        }
    }
}
