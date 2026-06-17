using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
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

    //pattern matched against each release asset's file name to find the full zip.
    //only '*' is a wildcard; matching is case-insensitive. delta assets are
    //identified separately by their name and are never treated as a full release.
    public string AssetPattern { get; init; } = "*.zip";

    //maximum number of successive delta steps to apply before preferring a single
    //full download instead. applying a very long chain is slower than one full
    //download, so beyond this many steps we just fetch the latest full zip.
    //set to 0 to disable deltas entirely.
    public int MaxDeltaSteps { get; init; } = 10;

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

//the target full release for a given check (also the fallback when deltas can't be used)
public sealed record UpdateInfo(SemVersion Version, string DownloadUrl, string AssetName);

//a single delta hop: applying it upgrades an install from From to To
public sealed record DeltaStep(SemVersion From, SemVersion To, string DownloadUrl, string AssetName);

//the chosen way to reach the latest version: either a delta chain or one full download
public sealed record UpdatePlan(
    SemVersion TargetVersion,
    bool UseDelta,
    UpdateInfo FullRelease,
    IReadOnlyList<DeltaStep> Steps)
{
    //how many download steps this plan will run: one per delta hop, or a single
    //full download. always >= 1, so it's safe to show as the "n" in "step x / n".
    public int StepCount => UseDelta ? Steps.Count : 1;
}

//progress for one download step within a plan. Step is 1-based; StepFraction is
//this step's own 0..1 progress; Overall is completion across the whole plan.
public readonly record struct DownloadProgress(int Step, int StepCount, double StepFraction)
{
    public double Overall => StepCount <= 0 ? 0 : (Step - 1 + Math.Clamp(StepFraction, 0, 1)) / StepCount;
}

public sealed class UpdateService
{
    //manifest file embedded in every delta zip; read for verification then stripped
    //so it never lands in the install directory.
    private const string DeltaManifestName = "delta-manifest.json";

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

    //precedence comparer (Semver v3 no longer implements IComparable)
    private static readonly IComparer<SemVersion> VersionComparer =
        Comparer<SemVersion>.Create((a, b) => a.ComparePrecedenceTo(b));

    //returns a plan to reach the newest release (delta chain or full), or null if up to date
    public async Task<UpdatePlan?> CheckAsync(CancellationToken ct = default)
    {
        var releases = await _github.Repository.Release
            .GetAll(_opt.Owner, _opt.Repo)
            .ConfigureAwait(false);

        //parse releases into version + full asset + (optional) delta asset
        var parsed = new List<ParsedRelease>();
        foreach (Release? rel in releases)
        {
            if (rel.Draft) continue;
            if (rel.Prerelease && !_opt.IncludePrereleases) continue;
            if (!SemVersion.TryParse(rel.TagName, SemVersionStyles.Any, out var ver) || ver is null) continue;

            UpdateInfo? full = null;
            DeltaStep? delta = null;
            foreach (var asset in rel.Assets)
            {
                if (TryParseDeltaName(asset.Name, out var from, out var to) && from is not null && to is not null)
                    delta = new DeltaStep(from, to, asset.BrowserDownloadUrl, asset.Name);
                else if (GlobMatch(asset.Name, _opt.AssetPattern))
                    full ??= new UpdateInfo(ver, asset.BrowserDownloadUrl, asset.Name);
            }

            if (full is not null || delta is not null)
                parsed.Add(new ParsedRelease(ver, full, delta));
        }

        var withFull = parsed.Where(p => p.Full is not null).OrderBy(p => p.Version, VersionComparer).ToList();
        if (withFull.Count == 0)
        {
            Trace.TraceInformation("No releases with a downloadable package found");
            return null;
        }

        var latest = withFull[^1];
        if (VersionComparer.Compare(latest.Version, CurrentVersion) <= 0)
        {
            Trace.TraceInformation("Already up to date. Current: {0}, Latest: {1}", CurrentVersion, latest.Version);
            return null;
        }

        //try to build a contiguous delta chain from CurrentVersion up to latest
        var ahead = parsed
            .Where(p => VersionComparer.Compare(p.Version, CurrentVersion) > 0)
            .OrderBy(p => p.Version, VersionComparer)
            .ToList();

        var steps = TryBuildDeltaChain(ahead, latest.Version);
        if (steps is not null)
        {
            Trace.TraceInformation("Updating via {0} delta step(s) from {1} to {2}", steps.Count, CurrentVersion, latest.Version);
            return new UpdatePlan(latest.Version, true, latest.Full!, steps);
        }

        Trace.TraceInformation("Updating via full download to {0}", latest.Version);
        return new UpdatePlan(latest.Version, false, latest.Full!, Array.Empty<DeltaStep>());
    }

    //returns an ordered delta chain that reaches target, or null if no usable chain exists
    private IReadOnlyList<DeltaStep>? TryBuildDeltaChain(List<ParsedRelease> ahead, SemVersion target)
    {
        if (_opt.MaxDeltaSteps <= 0 || ahead.Count == 0 || ahead.Count > _opt.MaxDeltaSteps)
            return null;

        var steps = new List<DeltaStep>(ahead.Count);
        var expected = CurrentVersion;
        foreach (var p in ahead)
        {
            //each release must carry a delta that goes from the version we're now at to itself
            if (p.Delta is null
                || VersionComparer.Compare(p.Delta.From, expected) != 0
                || VersionComparer.Compare(p.Delta.To, p.Version) != 0)
                return null;

            steps.Add(p.Delta);
            expected = p.Version;
        }

        //the chain must actually land on the latest version
        return VersionComparer.Compare(expected, target) == 0 ? steps : null;
    }

    //downloads the plan and extracts it into a single staged folder, returning that folder.
    //for a delta plan the steps are merged in order (later steps overwrite earlier ones),
    //which is equivalent to applying them one after another since nothing is deleted.
    public async Task<string> DownloadAsync(
        UpdatePlan plan, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
    {
        var root = Path.Combine(Environment.CurrentDirectory, "Updates", $"{_opt.Repo}-update-{plan.TargetVersion}");

        if (Directory.Exists(root))
        {
            //remove failed update attempt
            Directory.Delete(root, true);
        }

        Directory.CreateDirectory(root);
        var staged = Path.Combine(root, "staged");
        Directory.CreateDirectory(staged);

        if (!plan.UseDelta)
        {
            var zipPath = Path.Combine(root, plan.FullRelease.AssetName);
            var stepProgress = progress is null ? null : new StepProgress(progress, 1, 1);
            await DownloadZipAsync(plan.FullRelease.DownloadUrl, zipPath, stepProgress, ct).ConfigureAwait(false);
            ZipFile.ExtractToDirectory(zipPath, staged);
            File.Delete(zipPath);
            return staged;
        }

        for (var i = 0; i < plan.Steps.Count; i++)
        {
            var step = plan.Steps[i];
            IProgress<double>? stepProgress =
                progress is null ? null : new StepProgress(progress, i + 1, plan.Steps.Count);

            var zipPath = Path.Combine(root, step.AssetName);
            await DownloadZipAsync(step.DownloadUrl, zipPath, stepProgress, ct).ConfigureAwait(false);
            MergeDelta(zipPath, staged, step);
            File.Delete(zipPath);
        }

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
        psi.ArgumentList.Add("apply");      //selects the applier mode (vs. "delta" at build time)
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

    //parses a delta asset name of the form <prefix>-delta-<from>-to-<to>.zip.
    //version strings never contain "-delta-" or "-to-", so the split is unambiguous.
    private static bool TryParseDeltaName(string name, out SemVersion? from, out SemVersion? to)
    {
        from = null;
        to = null;

        const string marker = "-delta-";
        var i = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return false;

        var body = name[(i + marker.Length)..];
        if (body.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) body = body[..^4];

        var sep = body.IndexOf("-to-", StringComparison.Ordinal);
        if (sep < 0) return false;

        var fromText = body[..sep];
        var toText = body[(sep + "-to-".Length)..];

        if (SemVersion.TryParse(fromText, SemVersionStyles.Any, out var f) && f is not null
            && SemVersion.TryParse(toText, SemVersionStyles.Any, out var t) && t is not null)
        {
            from = f;
            to = t;
            return true;
        }
        return false;
    }

    private async Task DownloadZipAsync(string url, string destZip, IProgress<double>? progress, CancellationToken ct)
    {
        using var resp = await _http
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;

        await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var dst = File.Create(destZip);
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

    //verifies a delta's manifest matches the step it's supposed to be, then overlays
    //its files into the staged folder (the manifest itself is never copied).
    private static void MergeDelta(string deltaZip, string staged, DeltaStep step)
    {
        using var archive = ZipFile.OpenRead(deltaZip);

        var manifestEntry = archive.GetEntry(DeltaManifestName)
            ?? throw new InvalidOperationException($"Delta '{step.AssetName}' is missing {DeltaManifestName}.");

        using (var ms = manifestEntry.Open())
        using (var doc = JsonDocument.Parse(ms))
        {
            var from = doc.RootElement.GetProperty("from").GetString();
            var to = doc.RootElement.GetProperty("to").GetString();
            if (!SemVersion.TryParse(from, SemVersionStyles.Any, out var f) || f is null
                || !SemVersion.TryParse(to, SemVersionStyles.Any, out var t) || t is null
                || f.ComparePrecedenceTo(step.From) != 0
                || t.ComparePrecedenceTo(step.To) != 0)
                throw new InvalidOperationException(
                    $"Delta '{step.AssetName}' manifest ({from} -> {to}) does not match its name.");
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName == DeltaManifestName) continue;
            if (entry.FullName.EndsWith('/') || string.IsNullOrEmpty(entry.Name)) continue; //directory

            var rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            var dst = Path.Combine(staged, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            entry.ExtractToFile(dst, overwrite: true);
        }
    }

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

    //a release as parsed for planning: its version, full package (if any), delta (if any)
    private sealed record ParsedRelease(SemVersion Version, UpdateInfo? Full, DeltaStep? Delta);

    //tags a single download's 0..1 progress with which step it is, so the UI can
    //show "step x / n" as well as a percentage
    private sealed class StepProgress : IProgress<double>
    {
        private readonly IProgress<DownloadProgress> _inner;
        private readonly int _step;
        private readonly int _stepCount;

        public StepProgress(IProgress<DownloadProgress> inner, int step, int stepCount)
        {
            _inner = inner;
            _step = step;
            _stepCount = stepCount;
        }

        public void Report(double fraction) => _inner.Report(new DownloadProgress(_step, _stepCount, fraction));
    }
}