using System.Diagnostics;
using MicPilot.Core.Models;

namespace MicPilot.Profiles;

public sealed class ProcessWatcher
{
    private readonly object _cacheLock = new();
    private string[]? _cachedNames;
    private DateTime _cacheUtc = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMilliseconds(750);

    public bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var target = StripExe(processName);
        foreach (var running in GetRunningProcessNames())
        {
            if (MatchesProcessName(running, target))
            {
                return true;
            }
        }

        return false;
    }

    public Profile? FindRunningAutoActivateProfile(IEnumerable<Profile> profiles) =>
        profiles.FirstOrDefault(profile =>
            profile.Enabled &&
            profile.AutoActivate &&
            IsProcessRunning(profile.ProcessName));

    /// <summary>
    /// Exact match, plus FiveM/RedM build-tagged names
    /// (e.g. FiveM_GTAProcess ↔ FiveM_b3095_GTAProcess / FiveM_GameProcess / FiveM).
    /// </summary>
    public static bool MatchesProcessName(string runningName, string configuredName)
    {
        var running = StripExe(runningName);
        var target = StripExe(configuredName);

        if (string.IsNullOrWhiteSpace(running) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        if (running.Equals(target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsFiveMFamily(target))
        {
            return MatchesFiveM(running, target);
        }

        if (IsRedMFamily(target))
        {
            return MatchesRedM(running, target);
        }

        // Generic: Foo_Bar matches Foo_b123_Bar / Foo_anything_Bar
        var parts = target.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var prefix = parts[0];
            var suffix = parts[^1];
            if (running.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) &&
                running.EndsWith("_" + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesFiveM(string running, string target)
    {
        if (!running.StartsWith("FiveM", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Launcher / client shell counts as FiveM running
        if (running.Equals("FiveM", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // In-game (Legacy GTAProcess or Enhanced GameProcess), with or without build tag
        return ContainsToken(running, "GTAProcess") || ContainsToken(running, "GameProcess");
    }

    private static bool MatchesRedM(string running, string target)
    {
        if (!running.StartsWith("RedM", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (running.Equals("RedM", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ContainsToken(running, "GameProcess") || ContainsToken(running, "RDR2Process");
    }

    private static bool IsFiveMFamily(string name) =>
        name.Equals("FiveM", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("FiveM_", StringComparison.OrdinalIgnoreCase);

    private static bool IsRedMFamily(string name) =>
        name.Equals("RedM", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("RedM_", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsToken(string value, string token) =>
        value.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static string StripExe(string name)
    {
        var value = name.Trim();
        return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? value[..^4]
            : value;
    }

    private string[] GetRunningProcessNames()
    {
        lock (_cacheLock)
        {
            if (_cachedNames is not null && DateTime.UtcNow - _cacheUtc < CacheTtl)
            {
                return _cachedNames;
            }

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                return _cachedNames ?? [];
            }

            var names = new List<string>(processes.Length);
            foreach (var process in processes)
            {
                try
                {
                    names.Add(process.ProcessName);
                }
                catch
                {
                    // ignore inaccessible processes
                }
                finally
                {
                    process.Dispose();
                }
            }

            _cachedNames = names.ToArray();
            _cacheUtc = DateTime.UtcNow;
            return _cachedNames;
        }
    }
}
