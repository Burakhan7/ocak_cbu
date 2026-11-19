using Microsoft.EntityFrameworkCore.Migrations;

namespace UOM.Infrastructure.Persistence.Migrations;

/// <summary>
/// SQL Server Full-Text catalog. Skipped when Full-Text is not installed; search then uses LIKE.
/// </summary>
public static class FullTextSearchSql
{
    public static void Apply(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF CAST(SERVERPROPERTY('IsFullTextInstalled') AS int) = 1
               AND NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'UomSearchCatalog')
                EXEC(N'CREATE FULLTEXT CATALOG UomSearchCatalog AS DEFAULT');
            """,
            suppressTransaction: true);

        CreateIndex(
            migrationBuilder,
            "NewsPosts",
            "PK_NewsPosts",
            "[Title] LANGUAGE 1055, [Summary] LANGUAGE 1055, [BodyHtml] LANGUAGE 1055",
            "[Title] LANGUAGE 0, [Summary] LANGUAGE 0, [BodyHtml] LANGUAGE 0");
        CreateIndex(
            migrationBuilder,
            "Events",
            "PK_Events",
            "[Title] LANGUAGE 1055, [Summary] LANGUAGE 1055, [BodyHtml] LANGUAGE 1055",
            "[Title] LANGUAGE 0, [Summary] LANGUAGE 0, [BodyHtml] LANGUAGE 0");
        CreateIndex(
            migrationBuilder,
            "GuideArticles",
            "PK_GuideArticles",
            "[Title] LANGUAGE 1055, [Summary] LANGUAGE 1055, [BodyHtml] LANGUAGE 1055",
            "[Title] LANGUAGE 0, [Summary] LANGUAGE 0, [BodyHtml] LANGUAGE 0");
        CreateIndex(
            migrationBuilder,
            "Announcements",
            "PK_Announcements",
            "[Title] LANGUAGE 1055, [Body] LANGUAGE 1055",
            "[Title] LANGUAGE 0, [Body] LANGUAGE 0");
        CreateIndex(
            migrationBuilder,
            "Resources",
            "PK_Resources",
            "[Title] LANGUAGE 1055, [Description] LANGUAGE 1055, [CourseCode] LANGUAGE 1055",
            "[Title] LANGUAGE 0, [Description] LANGUAGE 0, [CourseCode] LANGUAGE 0");
    }

    public static void Remove(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "NewsPosts", "Events", "GuideArticles", "Announcements", "Resources" })
        {
            migrationBuilder.Sql(
                $"""
                IF CAST(SERVERPROPERTY('IsFullTextInstalled') AS int) = 1
                   AND EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'{table}'))
                    EXEC(N'DROP FULLTEXT INDEX ON [{table}]');
                """,
                suppressTransaction: true);
        }

        migrationBuilder.Sql(
            """
            IF CAST(SERVERPROPERTY('IsFullTextInstalled') AS int) = 1
               AND EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'UomSearchCatalog')
                EXEC(N'DROP FULLTEXT CATALOG UomSearchCatalog');
            """,
            suppressTransaction: true);
    }

    private static void CreateIndex(
        MigrationBuilder migrationBuilder,
        string table,
        string keyIndex,
        string turkishColumns,
        string neutralColumns)
    {
        migrationBuilder.Sql(
            $"""
            IF CAST(SERVERPROPERTY('IsFullTextInstalled') AS int) = 1
               AND NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'{table}'))
            BEGIN
                BEGIN TRY
                    EXEC(N'CREATE FULLTEXT INDEX ON [{table}] ({turkishColumns}) KEY INDEX [{keyIndex}] ON UomSearchCatalog WITH CHANGE_TRACKING AUTO');
                END TRY
                BEGIN CATCH
                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'{table}'))
                        EXEC(N'CREATE FULLTEXT INDEX ON [{table}] ({neutralColumns}) KEY INDEX [{keyIndex}] ON UomSearchCatalog WITH CHANGE_TRACKING AUTO');
                END CATCH
            END
            """,
            suppressTransaction: true);
    }
}
