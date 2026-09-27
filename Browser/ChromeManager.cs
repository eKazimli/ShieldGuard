using System.Diagnostics;

namespace ShieldGuard.Browser;

public class ChromeManager
{
    public string BrowserName => "Google Chrome";

    public bool IsInstalled()
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

        return paths.Any(File.Exists);
    }

    public bool IsRunning()
    {
        try
        {
            return Process.GetProcessesByName("chrome").Length > 0;
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
            return Process.GetProcessesByName("chrome").Length;
        }
        catch
        {
            return 0;
        }
    }
}