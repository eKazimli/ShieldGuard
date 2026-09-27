using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShieldGuard.Core;

public class FilterManager
{
    private readonly HashSet<string> blockedDomains =
        new(StringComparer.OrdinalIgnoreCase);

    public int FilterCount => blockedDomains.Count;

    public IReadOnlyCollection<string> Domains =>
        blockedDomains;

    public void Load(string filePath)
    {
        blockedDomains.Clear();

        if (!File.Exists(filePath))
        {
            return;
        }

        foreach (string rawLine in File.ReadAllLines(filePath))
        {
            string domain = NormalizeDomain(rawLine);

            if (!string.IsNullOrWhiteSpace(domain))
            {
                blockedDomains.Add(domain);
            }
        }
    }

    public bool IsBlocked(string host)
    {
        string normalizedHost = NormalizeHost(host);

        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return false;
        }

        if (blockedDomains.Contains(normalizedHost))
        {
            return true;
        }

        string[] parts = normalizedHost.Split('.');

        for (int i = 1; i < parts.Length - 1; i++)
        {
            string parentDomain = string.Join(
                ".",
                parts.Skip(i));

            if (blockedDomains.Contains(parentDomain))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeDomain(string value)
    {
        string domain = value.Trim();

        if (string.IsNullOrWhiteSpace(domain))
        {
            return string.Empty;
        }

        if (domain.StartsWith("#"))
        {
            return string.Empty;
        }

        if (domain.StartsWith("||"))
        {
            domain = domain[2..];
        }

        int separatorIndex = domain.IndexOfAny(
            new[]
            {
                '^',
                '/',
                ' '
            });

        if (separatorIndex >= 0)
        {
            domain = domain[..separatorIndex];
        }

        domain = domain.Trim();

        if (domain.StartsWith("*."))
        {
            domain = domain[2..];
        }

        return NormalizeHost(domain);
    }

    private static string NormalizeHost(string value)
    {
        string host = value
            .Trim()
            .ToLowerInvariant();

        if (host.StartsWith("www."))
        {
            host = host[4..];
        }

        host = host.TrimEnd('.');

        return host;
    }
}