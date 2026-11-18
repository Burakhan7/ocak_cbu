using FluentAssertions;
using UOM.Infrastructure.Persistence;

namespace UOM.Application.Tests.Persistence;

public class DatabaseInitializerTests
{
    [Theory]
    [InlineData(1801, true)]
    [InlineData(4060, true)]
    [InlineData(233, true)]
    [InlineData(18456, false)]
    [InlineData(547, false)]
    public void ShouldRetryMigration_for_expected_sql_errors(int number, bool expected) =>
        DatabaseInitializer.ShouldRetryMigration(number).Should().Be(expected);
}