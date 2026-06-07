using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using Octokit;
using Semver;

namespace DTXMania.Updater;

public sealed class UpdateOptions
{
    //GitHub repository owner
    public required string Owner { get; init; }

    //GitHub repository name
    public required string Repo { get; init; }

    //consider releases marked as pre-release
    public bool IncludePrereleases { get; init; } = true;
    
    //pattern matched against each release asset's file name to find the zip.
    //only '*' is a wildcard; matching is case-insensitive.
    public string AssetPattern { get; init; } = "*.zip";

    //paths (relative to the install root) the updater must never overwrite, even
    //if the package contains them. naming a folder preserves everything beneath it.
    //user files that simply aren't in the package are preserved automatically and
    //don't need listing. must stay in sync with the exclude list in CI packaging.
    public string[] PreservePaths { get; init; } = { "Config.ini" };

    //folder (relative to install root) that contains the applier executable
    public string ApplierDir { get; init; } = "updater";

    //file name (no path) of the main executable to relaunch after updating
    public string MainExe { get; init; } =
        OperatingSystem.IsWindows() ? "DTXManiaNX.exe" : "DTXManiaNX";

    //optional GitHub token. anonymous API access is limited to 60 requests/hour
    //per IP; a token raises that. normally unnecessary for a once-per-launch check.
    public string? Token { get; init; }
}

public sealed record UpdateInfo(SemVersion Version, string DownloadUrl, string AssetName);

public sealed class UpdateService
{
    private readonly UpdateOptions _opt;
    private readonly IGitHubClient _github;
    private readonly HttpClient _http;

    public UpdateService(UpdateOptions options, IGitHubClient? github = null, HttpClient? http = null)
    {
        _opt = options;
        _github = github ?? CreateGitHubClient(options);
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    private static IGitHubClient CreateGitHubClient(UpdateOptions opt)
    {
        var client = new GitHubClient(new ProductHeaderValue(SanitizeProduct(opt.Repo)));
        if (!string.IsNullOrEmpty(opt.Token))
            client.Credentials = new Credentials(opt.Token);
        return client;
    }

    //the running version, read from the stamped InformationalVersion
    public SemVersion CurrentVersion { get; } = ReadCurrentVersion();

    private static SemVersion ReadCurrentVersion()
    {
        var info = typeof(UpdateService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        //SemVersionStyles.Any tolerates a leading 'v' and the "+<sha>" the SDK appends.
        return SemVersion.TryParse(info, SemVersionStyles.Any, out var v) ? v : new SemVersion(0, 0, 0);
    }

    //returns the newest available update, or null if already up to date
    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        var releases = await _github.Repository.Release
            .GetAll(_opt.Owner, _opt.Repo)
            .ConfigureAwait(false);

        UpdateInfo? best = null;
        foreach (Release? rel in releases)
        {
            if (rel.Draft) continue;
            if (rel.Prerelease && !_opt.IncludePrereleases) continue;
            if (!SemVersion.TryParse(rel.TagName, SemVersionStyles.Any, out var ver)) continue;

            var asset = rel.Assets.FirstOrDefault(a => GlobMatch(a.Name, _opt.AssetPattern));
            if (asset is null) continue;

            if (best is null || ver.ComparePrecedenceTo(best.Version) > 0)
                best = new UpdateInfo(ver, asset.BrowserDownloadUrl, asset.Name);
        }

        // if (best is null || best.Version.ComparePrecedenceTo(CurrentVersion) <= 0)
        //     return null; //nothing newer than what we're running

        return best;
    }

    //downloads and extracts the update package to a fresh temporary staging folder,
    //returning the path to the extracted files.
    public async Task<string> DownloadAsync(
        UpdateInfo update, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var root = Path.Combine(Environment.CurrentDirectory, $"{_opt.Repo}-update-{update.Version}");

        if (Directory.Exists(root))
        {
            //remove failed update attempt
            Directory.Delete(root, true);
        }

        Directory.CreateDirectory(root);
        var zipPath = Path.Combine(root, update.AssetName);

        using (HttpResponseMessage resp = await _http
                   .GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                   .ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1L;

            await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var dst = File.Create(zipPath);
            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                readTotal += read;
                if (total > 0) progress?.Report((double)readTotal / total);
            }
        }

        var staged = Path.Combine(root, "staged");
        ZipFile.ExtractToDirectory(zipPath, staged);
        File.Delete(zipPath);
        return staged;
    }

    //copies the updater to a temp location (so the installed copy can itself be
    //replaced by the update), launches it, and returns. The caller MUST exit
    //promptly afterwards so the applier can overwrite locked files and relaunch.
    public void ApplyAndRestart(string stagedDir)
    {
        var install = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var updaterName = OperatingSystem.IsWindows() ? "updater.exe" : "updater";

        var updaterSrc = Path.Combine(install, _opt.ApplierDir);
        if (!Directory.Exists(updaterSrc))
            throw new DirectoryNotFoundException($"Updater folder not found: {updaterSrc}");

        var updaterTmp = Path.Combine(Path.GetTempPath(), $"{_opt.Repo}-updater-{Guid.NewGuid():N}");
        CopyDirectory(updaterSrc, updaterTmp);

        var updaterExe = Path.Combine(updaterTmp, updaterName);
        TrySetExecutable(updaterExe);

        var psi = new ProcessStartInfo
        {
            FileName = updaterExe,
            UseShellExecute = false,
            WorkingDirectory = updaterTmp,
        };
        psi.ArgumentList.Add("--staged");   psi.ArgumentList.Add(stagedDir);
        psi.ArgumentList.Add("--install");  psi.ArgumentList.Add(install);
        psi.ArgumentList.Add("--pid");      psi.ArgumentList.Add(Environment.ProcessId.ToString());
        psi.ArgumentList.Add("--relaunch"); psi.ArgumentList.Add(Path.Combine(install, _opt.MainExe));
        foreach (var preserve in _opt.PreservePaths)
        {
            psi.ArgumentList.Add("--preserve");
            psi.ArgumentList.Add(preserve);
        }

        Process.Start(psi);
    }

    // ---------- helpers ----------

    private static string SanitizeProduct(string s) => Regex.Replace(s, "[^A-Za-z0-9._-]", "-");

    private static bool GlobMatch(string name, string pattern)
    {
        var rx = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(name, rx, RegexOptions.IgnoreCase);
    }

    private static void CopyDirectory(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var dst = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(file, dst, true);
        }
    }

    private static void TrySetExecutable(string path)
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
}
