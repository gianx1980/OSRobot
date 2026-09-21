namespace OSRobot.Tests.IntegrationTests;

/// <summary>
/// Integration tests talk to real infrastructure (an SFTP/FTP server on localhost, a public REST API...),
/// so they are skipped unless explicitly enabled. Run them with:
///   set OSROBOT_INTEGRATION_TESTS=1   (or filter: dotnet test --filter TestCategory=Integration)
/// A default "dotnet test" therefore needs no network and no servers.
/// </summary>
internal static class IntegrationGuard
{
    public const string Category = "Integration";
    public const string EnvironmentVariable = "OSROBOT_INTEGRATION_TESTS";

    public static void RequireEnabled()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) != "1")
            Assert.Inconclusive($"Integration test skipped: set {EnvironmentVariable}=1 to run it against real servers.");
    }
}
