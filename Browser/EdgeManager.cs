using System.Diagnostics;

namespace ShieldGuard.Browser;

public class EdgeManager
{
    public string BrowserName => "Microsoft Edge";

    public bool IsInstalled()
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

        return paths.Any(File.Exists);
    }

    public bool IsRunning()
    {
        try
        {
            return Process.GetProcessesByName("msedge").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public int GetProcessCount()
    {
        try
        {
            return Process.GetProcessesByName("msedge").Length;
        }
        catch
        {
            return 0;
        }
    }
}