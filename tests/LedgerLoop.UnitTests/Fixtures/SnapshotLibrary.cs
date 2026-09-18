namespace LedgerLoop.UnitTests.Fixtures;

/// <summary>
/// Locates the committed characterization snapshots in the source tree so that a
/// recorded behaviour is diffable in version control.
/// </summary>
public static class SnapshotLibrary
{
    public static string Directory { get; } = Locate();

    private static string Locate()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "LedgerLoop.UnitTests", "Snapshots");
            if (System.IO.Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("tests/LedgerLoop.UnitTests/Snapshots could not be located from the test output directory.");
    }
}
