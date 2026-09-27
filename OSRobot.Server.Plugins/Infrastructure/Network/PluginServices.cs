/*======================================================================================
    Copyright 2025 by Gianluca Di Bucci (gianx1980) (https://www.os-robot.com)

    This file is part of OSRobot.

    OSRobot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    OSRobot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with OSRobot.  If not, see <http://www.gnu.org/licenses/>.
======================================================================================*/

using OSRobot.Server.Plugins.FtpSftpTask;

namespace OSRobot.Server.Plugins.Infrastructure.Network;

/// <summary>
/// Creation point for the network clients plugins use (HTTP, FTP/SFTP). Plugins are built with a
/// parameterless constructor and cloned for every execution, so constructor injection isn't an
/// option; they ask this class instead. By default it returns the real clients.
///
/// Tests replace them with <see cref="Override"/>. The override is scoped to the current async flow
/// (AsyncLocal), so tests running in parallel can each use their own fakes without interfering, and
/// it is undone when the returned scope is disposed.
/// </summary>
public static class PluginServices
{
    private sealed record Overrides(HttpMessageHandler? HttpHandler, Func<IFileTransferClient>? FileTransferClientFactory);

    private static readonly AsyncLocal<Overrides?> _overrides = new();

    /// <summary>New HttpClient. The caller disposes it; an overridden handler is NOT disposed with it.</summary>
    public static HttpClient CreateHttpClient()
    {
        HttpMessageHandler? handler = _overrides.Value?.HttpHandler;
        return handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    /// <summary>New FTP/SFTP client. The caller disposes it.</summary>
    public static IFileTransferClient CreateFileTransferClient()
    {
        Func<IFileTransferClient>? factory = _overrides.Value?.FileTransferClientFactory;
        return factory == null ? new FtpSftpClient() : factory();
    }

    /// <summary>
    /// Replaces the given services for the current async flow until the returned scope is disposed.
    /// Services passed as null keep their current (possibly already overridden) value.
    /// </summary>
    public static IDisposable Override(HttpMessageHandler? httpHandler = null, Func<IFileTransferClient>? fileTransferClientFactory = null)
    {
        Overrides? previous = _overrides.Value;
        _overrides.Value = new Overrides(httpHandler ?? previous?.HttpHandler,
                                         fileTransferClientFactory ?? previous?.FileTransferClientFactory);
        return new Scope(previous);
    }

    private sealed class Scope(Overrides? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _overrides.Value = previous;
        }
    }
}
