using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

// DTXMania updater
//
// One executable with two jobs, selected by the first argument:
//
//   apply - runs from a temporary copy of itself on a user's machine so it can
//           overwrite every file in the install directory, including the updater
//           itself. Waits for the game to exit, overlays the staged update onto
//           the install folder, then relaunches.
//
//   delta - build-time only (invoked by CI). Compares two release zips and writes
//           an incremental delta zip containing just the changed/new files, so the
//           updater can chain deltas and skip full downloads.
//
// Overlay semantics (to protect user data):
//   * every file in the staged package is copied over the install folder;
//   * does not touch any path covered by a --preserve entry;
//   * files already in the install folder that are not in the package are left
//     untouched - they are never deleted. So Config.ini, logs, user themes and
//     songs all survive.
//
// Args: apply --staged <dir> --install <dir> --pid <id> --relaunch <exePath>
//             [--preserve <relPath> ...]
//       delta --old <oldZip> --new <newZip> --from <ver> --to <ver> --out <deltaZip>

var argv = Environment.GetCommandLineArgs();
var verb = argv.Length > 1 ? argv[1] : "";

switch (verb)
{
    case "apply": return Apply();
    case "delta": return BuildDelta();
    default:
        Console.Error.WriteLine("usage: updater <apply|delta> [options]");
        return 2;
}

// ---------- apply (runtime) ----------
int Apply()
{
    string staged   = GetArg("--staged")   ?? Fail("--staged is required");
    string install  = GetArg("--install")  ?? Fail("--install is required");
    string relaunch = GetArg("--relaunch") ?? Fail("--relaunch is required");
    int    pid      = int.TryParse(GetArg("--pid"), out var parsed) ? parsed : -1;

    List<string> preserve = GetArgs("--preserve");

    string logPath = Path.Combine(install, "dtxmania-updater.log");

    Log($"Updater start. staged='{staged}' install='{install}' pid={pid} preserve=[{string.Join(", ", preserve)}]");

    try
    {
        WaitForExit(pid);
        Thread.Sleep(750); // grace period so the OS releases file handles

        int copied = 0, preserved = 0;
        foreach (var srcFile in Directory.EnumerateFiles(staged, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(staged, srcFile);
            if (IsPreserved(rel, preserve)) { preserved++; continue; }

            var dst = Path.Combine(install, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            CopyWithRetry(srcFile, dst);
            copied++;
        }
        Log($"Overlay complete. copied={copied} preserved={preserved}");

        if (!OperatingSystem.IsWindows()) TrySetExecutable(relaunch);

        Process.Start(new ProcessStartInfo
        {
            FileName = relaunch,
            WorkingDirectory = install,
            UseShellExecute = false,
        });
        Log("Relaunched application.");
    }
    catch (Exception ex)
    {
        Log("ERROR: " + ex);
        // Last resort: try to relaunch whatever is there so the user isn't left with nothing.
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = relaunch,
                WorkingDirectory = install,
                UseShellExecute = false,
            });
        }
        catch { /* nothing more we can do */ }
    }
    finally
    {
        TryCleanupStagingRoot(staged);
    }

    return 0;

    // local to apply so it can capture the install log path
    void Log(string message)
    {
        try { File.AppendAllText(logPath, $"{DateTime.Now:O}  {message}{Environment.NewLine}"); }
        catch { /* logging must never throw */ }
    }
}

// ---------- delta (build time) ----------
int BuildDelta()
{
    string oldZip = GetArg("--old")  ?? Fail("--old is required");
    string newZip = GetArg("--new")  ?? Fail("--new is required");
    string from   = GetArg("--from") ?? Fail("--from is required");
    string to     = GetArg("--to")   ?? Fail("--to is required");
    string outZip = GetArg("--out")  ?? Fail("--out is required");

    const string manifestName = "delta-manifest.json";

    // hash every file in the old zip. comparing CONTENT hashes means a file that
    // was only re-timestamped does not count as a change.
    var oldHashes = new Dictionary<string, string>(StringComparer.Ordinal);
    using (var old = ZipFile.OpenRead(oldZip))
        foreach (var e in old.Entries)
        {
            if (IsDirectory(e)) continue;
            oldHashes[e.FullName] = HashEntry(e);
        }

    var changed = new List<object>();
    if (File.Exists(outZip)) File.Delete(outZip);

    // walk the new zip; carry over only entries that are new or whose content differs
    using (var newArchive = ZipFile.OpenRead(newZip))
    using (var deltaStream = File.Create(outZip))
    using (var delta = new ZipArchive(deltaStream, ZipArchiveMode.Create))
    {
        foreach (var e in newArchive.Entries)
        {
            if (IsDirectory(e)) continue;
            if (e.FullName == manifestName) continue;

            var hash = HashEntry(e);
            if (oldHashes.TryGetValue(e.FullName, out var oldHash) && oldHash == hash)
                continue; // identical content - not part of the delta

            var de = delta.CreateEntry(e.FullName, CompressionLevel.Optimal);
            using (var src = e.Open())
            using (var dst = de.Open())
                src.CopyTo(dst);

            changed.Add(new { path = e.FullName, sha256 = hash });
        }

        // embed from/to so the updater can confirm a delta composes onto the version
        // it is upgrading before it applies anything
        var manifest = new { from, to, files = changed };
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        var me = delta.CreateEntry(manifestName, CompressionLevel.Optimal);
        using var writer = new StreamWriter(me.Open());
        writer.Write(json);
    }

    Console.WriteLine($"Delta written: {outZip}  ({changed.Count} changed/new file(s))  {from} -> {to}");
    return 0;
}

// ---------- helpers ----------

static void WaitForExit(int pid)
{
    if (pid <= 0) return;
    try
    {
        var proc = Process.GetProcessById(pid);
        proc.WaitForExit(60_000); // proceed even if it overruns; better than hanging forever
    }
    catch (ArgumentException) { /* process already gone */ }
}

static void CopyWithRetry(string src, string dst)
{
    const int attempts = 10;
    for (var i = 1; i <= attempts; i++)
    {
        try { File.Copy(src, dst, true); return; }
        catch (IOException) when (i < attempts) { Thread.Sleep(300); }
        catch (UnauthorizedAccessException) when (i < attempts) { Thread.Sleep(300); }
    }
    File.Copy(src, dst, true); // let the final failure surface
}

static bool IsPreserved(string relPath, IReadOnlyList<string> preserve)
{
    var norm = relPath.Replace('\\', '/');
    var cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    foreach (var entry in preserve)
    {
        var p = entry.Replace('\\', '/').TrimEnd('/');
        if (p.Length == 0) continue;
        if (norm.Equals(p, cmp)) return true;            // exact file match
        if (norm.StartsWith(p + "/", cmp)) return true;  // anything inside a preserved folder
    }
    return false;
}

static void TrySetExecutable(string path)
{
    if (OperatingSystem.IsWindows()) return; // no-op + silences the platform analyzer
    try
    {
        if (File.Exists(path))
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
    catch { /* best effort */ }
}

static void TryCleanupStagingRoot(string staged)
{
    try
    {
        var root = Directory.GetParent(staged)?.FullName;
        if (root is not null && Directory.Exists(root)) Directory.Delete(root, true);
    }
    catch { /* leftover temp files are harmless */ }
}

static bool IsDirectory(ZipArchiveEntry e) => e.FullName.EndsWith('/') || string.IsNullOrEmpty(e.Name);

static string HashEntry(ZipArchiveEntry e)
{
    using var s = e.Open();
    using var sha = SHA256.Create();
    return Convert.ToHexString(sha.ComputeHash(s));
}

static string? GetArg(string key)
{
    var args = Environment.GetCommandLineArgs();
    for (var i = 1; i < args.Length - 1; i++)
        if (args[i] == key) return args[i + 1];
    return null;
}

static List<string> GetArgs(string key)
{
    var result = new List<string>();
    var args = Environment.GetCommandLineArgs();
    for (var i = 1; i < args.Length - 1; i++)
        if (args[i] == key) result.Add(args[i + 1]);
    return result;
}

static string Fail(string message) => throw new ArgumentException(message);