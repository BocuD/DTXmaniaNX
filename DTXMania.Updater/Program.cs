using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

// =============================================================================
// DTXMania update applier (standalone).
//
// Runs from a temporary copy of itself so it can overwrite every file in the
// install directory - including its own installed copy. It waits for the game to
// exit, overlays the staged update onto the install folder, then relaunches.
//
// Overlay semantics (this is what protects user data):
//   * every file in the staged package is copied over the install folder;
//   * EXCEPT any path covered by a --preserve entry;
//   * files already in the install folder that are NOT in the package are left
//     untouched - they are never deleted. So Config.ini, logs, user themes and
//     user-added songs all survive.
//
// Args: --staged <dir> --install <dir> --pid <id> --relaunch <exePath>
//       [--preserve <relPath> ...]
// =============================================================================

string staged   = GetArg("--staged")   ?? Fail("--staged is required");
string install  = GetArg("--install")  ?? Fail("--install is required");
string relaunch = GetArg("--relaunch") ?? Fail("--relaunch is required");
int    pid      = int.TryParse(GetArg("--pid"), out var parsed) ? parsed : -1;
List<string> preserve = GetArgs("--preserve");

string logPath = Path.Combine(Path.GetTempPath(), "dtxmania-updater.log");
void Log(string message)
{
    try { File.AppendAllText(logPath, $"{DateTime.Now:O}  {message}{Environment.NewLine}"); }
    catch { /* logging must never throw */ }
}

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

return;

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
