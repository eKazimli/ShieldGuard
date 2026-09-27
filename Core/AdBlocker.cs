using System;

namespace ShieldGuard.Core;

public class AdBlocker
{
    private readonly FilterManager filterManager;

    public long BlockedCount { get; private set; }

    public AdBlocker(
        FilterManager filterManager)
    {
        this.filterManager =
            filterManager;
    }

    public bool ShouldBlock(
        string url)
    {
        if (string.IsNullOrWhiteSpace(
            url))
        {
            return false;
        }

        if (!Uri.TryCreate(
            url,
            UriKind.Absolute,
            out Uri? uri))
        {
            return false;
        }

        if (!string.Equals(
                uri.Scheme,
                "http",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                uri.Scheme,
                "https",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool blocked =
            filterManager.IsBlocked(
                uri.Host);

        if (blocked)
        {
            BlockedCount++;
        }

        return blocked;
    }

    public void ResetStatistics()
    {
        BlockedCount = 0;
    }
}