using System;
using System.Diagnostics;
using System.IO;

namespace ShieldGuard.Core;

public class ProtectionManager : IDisposable
{
    private readonly FilterManager filterManager;
    private readonly AdBlocker adBlocker;
    private readonly LocalStatusServer localStatusServer;

    private bool isRunning;
    private long browserBlockedCount;

    public bool IsRunning => isRunning;

    public int FilterCount => filterManager.FilterCount;

    public long BlockedCount => browserBlockedCount;

    public long InternalBlockedCount => adBlocker.BlockedCount;

    public bool LocalServerRunning => localStatusServer.IsRunning;

    public int InstalledBrowserCount
    {
        get
        {
            int count = 0;

            if (IsChromeInstalled())
            {
                count++;
            }

            if (IsEdgeInstalled())
            {
                count++;
            }

            return count;
        }
    }

    public int RunningBrowserProcessCount
    {
        get
        {
            return GetProcessCount("chrome") +
                   GetProcessCount("msedge");
        }
    }

    public ProtectionManager()
    {
        filterManager = new FilterManager();
        adBlocker = new AdBlocker(filterManager);

        localStatusServer = new LocalStatusServer(
            () => IsRunning,
            () => filterManager.Domains,
            () => BlockedCount,
            UpdateBrowserBlockedCount);

        localStatusServer.Start();
    }

    public void Start()
    {
        string filterPath = GetFilterFilePath();

        filterManager.Load(filterPath);

        adBlocker.ResetStatistics();

        browserBlockedCount = 0;

        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;

        browserBlockedCount = 0;
    }

    public void UpdateBrowserBlockedCount(long count)
    {
        if (!isRunning)
        {
            return;
        }

        if (count < 0)
        {
            count = 0;
        }

        browserBlockedCount = count;
    }

    public bool ShouldBlock(string url)
    {
        if (!isRunning)
        {
            return false;
        }

        return adBlocker.ShouldBlock(url);
    }

    public int GetBrowserCount()
    {
        return InstalledBrowserCount;
    }

    public void Dispose()
    {
        Stop();
        localStatusServer.Dispose();
    }

    private static int GetProcessCount(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName).Length;
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsChromeInstalled()
    {
        string[] paths =
        {
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe")
        };

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEdgeInstalled()
    {
        string[] paths =
        {
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles),
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "Edge",
                "Application",
                "msedge.exe")
        };

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetFilterFilePath()
    {
        string currentDirectory = AppContext.BaseDirectory;

        for (int i = 0; i < 6; i++)
        {
            string candidate = Path.Combine(
                currentDirectory,
                "Filters",
                "filters.txt");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent =
                Directory.GetParent(currentDirectory);

            if (parent == null)
            {
                break;
            }

            currentDirectory = parent.FullName;
        }

        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "Filters",
            "filters.txt");
    }
}