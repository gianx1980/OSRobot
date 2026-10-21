// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OSRobot.Tests.Fakes;

/// <summary>
/// A real HTTPS endpoint on a random loopback port, with a self-signed certificate made on the fly:
/// clients that validate certificates fail with UntrustedRoot, like a dev server's certificate.
/// Answers every request with 200 and the given JSON.
/// </summary>
public sealed class SelfSignedHttpsServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private readonly byte[] _response;
    private readonly CancellationTokenSource _stopping = new();

    public string Url { get; }

    public SelfSignedHttpsServer(string responseJson)
    {
        _certificate = CreateCertificate();

        byte[] body = Encoding.UTF8.GetBytes(responseJson);
        _response = [.. Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n"), .. body];

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Url = $"https://localhost:{((IPEndPoint)_listener.LocalEndpoint).Port}/api/login";

        _ = AcceptLoopAsync();
    }

    private static X509Certificate2 CreateCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder subjectAlternativeName = new();
        subjectAlternativeName.AddDnsName("localhost");
        request.CertificateExtensions.Add(subjectAlternativeName.Build());

        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // SChannel (Windows) cannot do a server handshake with an ephemeral key: round-trip through PFX.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stopping.Token);
            }
            catch
            {
                return;
            }

            _ = HandleAsync(client);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                using SslStream ssl = new(client.GetStream());
                await ssl.AuthenticateAsServerAsync(_certificate);

                await ReadRequestAsync(ssl);
                await ssl.WriteAsync(_response);
                await ssl.FlushAsync();
            }
            catch
            {
                // The client rejected the certificate: the handshake fails, which is the point.
            }
        }
    }

    // Reads the headers and, if any, the body, so the client isn't reset while still sending.
    private static async Task ReadRequestAsync(Stream stream)
    {
        List<byte> received = [];
        byte[] buffer = new byte[4096];
        int headerEnd = -1;

        while (headerEnd < 0)
        {
            int read = await stream.ReadAsync(buffer);
            if (read == 0)
                return;
            received.AddRange(buffer.AsSpan(0, read).ToArray());
            headerEnd = IndexOfHeaderEnd(received);
        }

        string headers = Encoding.ASCII.GetString([.. received.Take(headerEnd)]);
        string? lengthLine = headers.Split("\r\n").FirstOrDefault(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
        int contentLength = lengthLine == null ? 0 : int.Parse(lengthLine["Content-Length:".Length..].Trim());

        int bodyReceived = received.Count - (headerEnd + 4);
        while (bodyReceived < contentLength)
        {
            int read = await stream.ReadAsync(buffer);
            if (read == 0)
                return;
            bodyReceived += read;
        }
    }

    private static int IndexOfHeaderEnd(List<byte> data)
    {
        for (int i = 0; i + 3 < data.Count; i++)
        {
            if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                return i;
        }
        return -1;
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _listener.Stop();
        _certificate.Dispose();
        _stopping.Dispose();
    }
}
