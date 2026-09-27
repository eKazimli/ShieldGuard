using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ShieldGuard.Core;

public class LocalStatusServer : IDisposable
{
    private readonly Func<bool> isRunningProvider;
    private readonly Func<IReadOnlyCollection<string>> domainsProvider;
    private readonly Func<long> blockedCountProvider;
    private readonly Action<long> blockedCountUpdater;

    private readonly TcpListener listener;
    private CancellationTokenSource? cancellationTokenSource;
    private Task? serverTask;

    public bool IsRunning { get; private set; }

    public LocalStatusServer(
        Func<bool> isRunningProvider,
        Func<IReadOnlyCollection<string>> domainsProvider,
        Func<long> blockedCountProvider,
        Action<long> blockedCountUpdater)
    {
        this.isRunningProvider = isRunningProvider;
        this.domainsProvider = domainsProvider;
        this.blockedCountProvider = blockedCountProvider;
        this.blockedCountUpdater = blockedCountUpdater;

        listener = new TcpListener(
            IPAddress.Loopback,
            17845);
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();

        listener.Start();

        IsRunning = true;

        serverTask = RunServerAsync(
            cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;

        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch
        {
        }

        try
        {
            listener.Stop();
        }
        catch
        {
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        serverTask = null;
    }

    private async Task RunServerAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;

            try
            {
                client = await listener.AcceptTcpClientAsync(
                    cancellationToken);

                _ = Task.Run(
                    () => HandleClientAsync(
                        client,
                        cancellationToken),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                client?.Dispose();

                if (!IsRunning)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();

                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;

                byte[] buffer = new byte[16384];

                int totalRead = 0;
                int headerEnd = -1;

                while (totalRead < buffer.Length)
                {
                    int read = await stream.ReadAsync(
                        buffer.AsMemory(
                            totalRead,
                            buffer.Length - totalRead),
                        cancellationToken);

                    if (read <= 0)
                    {
                        return;
                    }

                    totalRead += read;

                    headerEnd = FindHeaderEnd(
                        buffer,
                        totalRead);

                    if (headerEnd >= 0)
                    {
                        break;
                    }
                }

                if (headerEnd < 0)
                {
                    await SendResponseAsync(
                        stream,
                        400,
                        "Bad Request",
                        "{\"error\":\"Invalid HTTP request\"}",
                        cancellationToken);

                    return;
                }

                string headerText = Encoding.UTF8.GetString(
                    buffer,
                    0,
                    headerEnd);

                string[] headerLines =
                    headerText.Split(
                        new[] { "\r\n" },
                        StringSplitOptions.None);

                if (headerLines.Length == 0)
                {
                    return;
                }

                string[] requestLineParts =
                    headerLines[0].Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

                if (requestLineParts.Length < 2)
                {
                    await SendResponseAsync(
                        stream,
                        400,
                        "Bad Request",
                        "{\"error\":\"Invalid request line\"}",
                        cancellationToken);

                    return;
                }

                string method = requestLineParts[0]
                    .Trim()
                    .ToUpperInvariant();

                string path = requestLineParts[1]
                    .Trim();

                int contentLength =
                    GetContentLength(headerLines);

                int bodyStart = headerEnd + 4;

                int currentBodyLength =
                    Math.Max(
                        0,
                        totalRead - bodyStart);

                StringBuilder bodyBuilder =
                    new StringBuilder();

                if (currentBodyLength > 0)
                {
                    bodyBuilder.Append(
                        Encoding.UTF8.GetString(
                            buffer,
                            bodyStart,
                            currentBodyLength));
                }

                int remaining =
                    contentLength - currentBodyLength;

                while (remaining > 0)
                {
                    byte[] bodyBuffer =
                        new byte[Math.Min(8192, remaining)];

                    int read = await stream.ReadAsync(
                        bodyBuffer.AsMemory(
                            0,
                            bodyBuffer.Length),
                        cancellationToken);

                    if (read <= 0)
                    {
                        break;
                    }

                    bodyBuilder.Append(
                        Encoding.UTF8.GetString(
                            bodyBuffer,
                            0,
                            read));

                    remaining -= read;
                }

                string body = bodyBuilder.ToString();

                await ProcessRequestAsync(
                    stream,
                    method,
                    path,
                    body,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }
        }
    }

    private async Task ProcessRequestAsync(
        NetworkStream stream,
        string method,
        string path,
        string body,
        CancellationToken cancellationToken)
    {
        if (method == "OPTIONS")
        {
            await SendResponseAsync(
                stream,
                204,
                "No Content",
                string.Empty,
                cancellationToken);

            return;
        }

        if (method == "GET" &&
            path.Equals(
                "/status",
                StringComparison.OrdinalIgnoreCase))
        {
            object response = new
            {
                active = isRunningProvider(),
                application = "ShieldGuard",
                version = "1.0.0"
            };

            await SendJsonAsync(
                stream,
                200,
                response,
                cancellationToken);

            return;
        }

        if (method == "GET" &&
            path.Equals(
                "/rules",
                StringComparison.OrdinalIgnoreCase))
        {
            object response = new
            {
                active = isRunningProvider(),
                domains = domainsProvider()
                    .OrderBy(x => x)
                    .ToArray()
            };

            await SendJsonAsync(
                stream,
                200,
                response,
                cancellationToken);

            return;
        }

        if (method == "GET" &&
            path.Equals(
                "/statistics",
                StringComparison.OrdinalIgnoreCase))
        {
            object response = new
            {
                active = isRunningProvider(),
                blockedAds = blockedCountProvider()
            };

            await SendJsonAsync(
                stream,
                200,
                response,
                cancellationToken);

            return;
        }

        if (method == "POST" &&
            path.Equals(
                "/statistics",
                StringComparison.OrdinalIgnoreCase))
        {
            await ProcessStatisticsPostAsync(
                stream,
                body,
                cancellationToken);

            return;
        }

        await SendJsonAsync(
            stream,
            404,
            new
            {
                error = "Endpoint not found"
            },
            cancellationToken);
    }

    private async Task ProcessStatisticsPostAsync(
        NetworkStream stream,
        string body,
        CancellationToken cancellationToken)
    {
        if (!isRunningProvider())
        {
            await SendJsonAsync(
                stream,
                403,
                new
                {
                    error = "ShieldGuard protection is inactive"
                },
                cancellationToken);

            return;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            await SendJsonAsync(
                stream,
                400,
                new
                {
                    error = "Request body is empty"
                },
                cancellationToken);

            return;
        }

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(body);

            JsonElement root =
                document.RootElement;

            if (!root.TryGetProperty(
                    "blockedAds",
                    out JsonElement blockedAdsElement))
            {
                await SendJsonAsync(
                    stream,
                    400,
                    new
                    {
                        error = "blockedAds is required"
                    },
                    cancellationToken);

                return;
            }

            long count;

            if (blockedAdsElement.ValueKind ==
                JsonValueKind.Number &&
                blockedAdsElement.TryGetInt64(
                    out long numericValue))
            {
                count = numericValue;
            }
            else
            {
                await SendJsonAsync(
                    stream,
                    400,
                    new
                    {
                        error = "blockedAds must be a number"
                    },
                    cancellationToken);

                return;
            }

            if (count < 0)
            {
                count = 0;
            }

            blockedCountUpdater(count);

            await SendJsonAsync(
                stream,
                200,
                new
                {
                    success = true,
                    blockedAds = blockedCountProvider()
                },
                cancellationToken);
        }
        catch (JsonException)
        {
            await SendJsonAsync(
                stream,
                400,
                new
                {
                    error = "Invalid JSON"
                },
                cancellationToken);
        }
    }

    private static int FindHeaderEnd(
        byte[] buffer,
        int length)
    {
        for (int i = 0; i <= length - 4; i++)
        {
            if (buffer[i] == 13 &&
                buffer[i + 1] == 10 &&
                buffer[i + 2] == 13 &&
                buffer[i + 3] == 10)
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetContentLength(
        string[] headers)
    {
        foreach (string header in headers)
        {
            if (!header.StartsWith(
                    "Content-Length:",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value =
                header["Content-Length:".Length..]
                .Trim();

            if (int.TryParse(
                    value,
                    out int contentLength))
            {
                return Math.Max(
                    0,
                    contentLength);
            }
        }

        return 0;
    }

    private static async Task SendJsonAsync(
        NetworkStream stream,
        int statusCode,
        object data,
        CancellationToken cancellationToken)
    {
        string json =
            JsonSerializer.Serialize(data);

        await SendResponseAsync(
            stream,
            statusCode,
            GetStatusText(statusCode),
            json,
            cancellationToken);
    }

    private static async Task SendResponseAsync(
        NetworkStream stream,
        int statusCode,
        string statusText,
        string body,
        CancellationToken cancellationToken)
    {
        byte[] bodyBytes =
            Encoding.UTF8.GetBytes(body);

        StringBuilder response =
            new StringBuilder();

        response.Append(
            $"HTTP/1.1 {statusCode} {statusText}\r\n");

        response.Append(
            "Content-Type: application/json; charset=utf-8\r\n");

        response.Append(
            "Access-Control-Allow-Origin: *\r\n");

        response.Append(
            "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n");

        response.Append(
            "Access-Control-Allow-Headers: Content-Type\r\n");

        response.Append(
            $"Content-Length: {bodyBytes.Length}\r\n");

        response.Append(
            "Connection: close\r\n");

        response.Append(
            "\r\n");

        byte[] headerBytes =
            Encoding.UTF8.GetBytes(
                response.ToString());

        await stream.WriteAsync(
            headerBytes,
            cancellationToken);

        if (bodyBytes.Length > 0)
        {
            await stream.WriteAsync(
                bodyBytes,
                cancellationToken);
        }

        await stream.FlushAsync(
            cancellationToken);
    }

    private static string GetStatusText(
        int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            204 => "No Content",
            400 => "Bad Request",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "OK"
        };
    }

    public void Dispose()
    {
        Stop();
    }
}