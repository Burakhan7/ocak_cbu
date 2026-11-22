using System.Runtime.CompilerServices;

namespace UOM.Web.IntegrationTests;

internal static class ParentSiteFetchGate
{
    [ModuleInitializer]
    internal static void DisableLiveParentFetch()
    {
        Environment.SetEnvironmentVariable("ParentSite__Enabled", "false");
    }
}
