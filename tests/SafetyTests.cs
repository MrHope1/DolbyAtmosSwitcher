using System;
using System.IO;

internal static class SafetyTests
{
    private static int failures;

    private static void AssertTrue(bool condition, string message)
    {
        if (condition)
        {
            Console.WriteLine("PASS: " + message);
            return;
        }

        failures++;
        Console.Error.WriteLine("FAIL: " + message);
    }

    private static void TestExplorerPath()
    {
        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "explorer.exe");
        string actual = WindowsSystemPaths.ExplorerExecutablePath;

        AssertTrue(Path.IsPathRooted(actual), "Explorer path is absolute");
        AssertTrue(
            string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase),
            "Explorer path resolves to the Windows directory");
        AssertTrue(File.Exists(actual), "Resolved Explorer executable exists");
    }

    private static void TestCliProfiles()
    {
        string profile;
        AssertTrue(
            CliArguments.TryGetProfile(new[] { "/change", "music" }, out profile) && profile == "Music",
            "CLI accepts and canonicalizes /change music");
        AssertTrue(
            CliArguments.TryGetProfile(new[] { "-c", "VOICE" }, out profile) && profile == "Voice",
            "CLI accepts and canonicalizes -c VOICE");
        AssertTrue(
            !CliArguments.TryGetProfile(new[] { "/change", "NotAProfile" }, out profile),
            "CLI rejects an unknown profile");
        AssertTrue(
            !CliArguments.TryGetProfile(new[] { "/change" }, out profile),
            "CLI rejects a missing profile");
    }

    private static void TestLogLocation()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logPath = Logger.LogFilePath;

        AssertTrue(Path.IsPathRooted(logPath), "Log path is absolute");
        AssertTrue(
            logPath.StartsWith(localAppData + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Log path is under per-user LocalAppData");
        AssertTrue(
            !string.Equals(
                Path.GetDirectoryName(logPath),
                AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase),
            "Log is not written beside the executable");
    }

#if TESTING
    private static void TestLogOverride()
    {
        const string variableName = "DOLBY_SWITCHER_TEST_LOG_DIRECTORY";
        string originalValue = Environment.GetEnvironmentVariable(variableName);
        string testDirectory = Path.Combine(Path.GetTempPath(), "DolbySwitcher-LogOverride-Test");

        try
        {
            Environment.SetEnvironmentVariable(variableName, testDirectory);
            AssertTrue(
                string.Equals(
                    Logger.LogFilePath,
                    Path.Combine(testDirectory, "debug_log.txt"),
                    StringComparison.OrdinalIgnoreCase),
                "Test builds can isolate runtime logging in a temporary directory");
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
#endif

    private static void TestAutomationFallback()
    {
        AssertTrue(
            !DolbyAccessAutomator.InvokeElement(null),
            "A missing automation element cannot report success");
    }

    private static void TestProfileState()
    {
        string[] profiles = new[] { "Game", "Movie", "Music", "Voice" };
        AssertTrue(
            ProfileState.ResolveIndex(profiles, "Movie", 0, false) == 0,
            "A failed profile switch preserves the current tray state");
        AssertTrue(
            ProfileState.ResolveIndex(profiles, "Movie", 0, true) == 1,
            "A successful profile switch commits the selected tray state");
    }

    private static int Main()
    {
        TestExplorerPath();
        TestCliProfiles();
        TestLogLocation();
#if TESTING
        TestLogOverride();
#endif
        TestAutomationFallback();
        TestProfileState();

        if (failures == 0)
        {
            Console.WriteLine("All core safety tests passed.");
            return 0;
        }

        Console.Error.WriteLine(failures + " core safety test(s) failed.");
        return 1;
    }
}
