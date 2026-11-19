using FluentAssertions;
using UOM.Infrastructure.Features.Admin;
using Xunit;

namespace UOM.Application.Tests.Features.Admin;

public class VideoAdminServiceTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void NormalizeYouTubeId_extracts_id(string input, string expected)
    {
        VideoAdminService.NormalizeYouTubeId(input).Should().Be(expected);
    }
}