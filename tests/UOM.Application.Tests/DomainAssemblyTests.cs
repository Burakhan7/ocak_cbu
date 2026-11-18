using FluentAssertions;
using UOM.Domain;

namespace UOM.Application.Tests;

public class DomainAssemblyTests
{
    [Fact]
    public void DomainAssembly_marker_type_exists()
    {
        typeof(DomainAssembly).Should().NotBeNull();
        typeof(DomainAssembly).IsAbstract.Should().BeTrue();
        typeof(DomainAssembly).IsSealed.Should().BeTrue();
    }
}