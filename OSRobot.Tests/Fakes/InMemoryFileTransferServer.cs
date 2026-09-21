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
using System.Text;

namespace OSRobot.Tests.Fakes;

/// <summary>
/// An in-memory stand-in for an FTP/SFTP server: a remote file system plus credentials. Every
/// connection made through <see cref="CreateClient"/> shares it, so state written by one task run is
/// visible to the next, exactly as with a real server.
///
/// It mimics the strict behaviours of real servers that matter for the task logic: a directory can
/// only be created inside an existing one, an upload needs its parent directory, an existing file
/// is not silently overwritten, and asking about a missing path throws.
/// </summary>
public sealed class InMemoryFileTransferServer
{
    private readonly object _gate = new();
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal) { "/" };
    private readonly List<string> _operations = [];

    public string Username { get; init; } = "test";
    public string Password { get; init; } = "12345";

    /// <summary>Every remote operation performed, in order, e.g. "MKDIR /a", "UPLOAD /a/b.txt".</summary>
    public IReadOnlyList<string> Operations { get { lock (_gate) return [.. _operations]; } }

    public IFileTransferClient CreateClient() => new Client(this);

    // --- Inspection helpers for assertions ---------------------------------------------------

    public bool FileExists(string path) { lock (_gate) return _files.ContainsKey(Normalize(path)); }
    public bool DirectoryExists(string path) { lock (_gate) return _directories.Contains(Normalize(path)); }
    public string ReadAllText(string path) { lock (_gate) return Encoding.UTF8.GetString(_files[Normalize(path)]); }
    public List<string> AllFiles() { lock (_gate) return [.. _files.Keys.Order(StringComparer.Ordinal)]; }

    /// <summary>Seeds a remote file (and its parent directories) without going through a client.</summary>
    public void SeedFile(string path, string content)
    {
        lock (_gate)
        {
            string p = Normalize(path);
            EnsureDirectories(ParentOf(p));
            _files[p] = Encoding.UTF8.GetBytes(content);
        }
    }

    // --- Internals ---------------------------------------------------------------------------

    // Real servers see forward-slash absolute paths; the task passes a mix of "\x", "/x" and "x/".
    private static string Normalize(string path)
    {
        string p = "/" + path.Replace('\\', '/').Trim('/');
        return p.Length > 1 ? p.TrimEnd('/') : p;
    }

    private static string ParentOf(string normalized)
    {
        int i = normalized.LastIndexOf('/');
        return i <= 0 ? "/" : normalized[..i];
    }

    private void EnsureDirectories(string normalized)
    {
        string current = string.Empty;
        foreach (string segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current += "/" + segment;
            _directories.Add(current);
        }
    }

    private sealed class Client(InMemoryFileTransferServer server) : IFileTransferClient
    {
        private bool _connected;

        private void Log(string op) => server._operations.Add(op);

        private void RequireConnected()
        {
            if (!_connected)
                throw new InvalidOperationException("Method \"Connect\" not called.");
        }

        public void Connect(ProtocolEnum protocol, string host, int port, string username, string password)
        {
            lock (server._gate)
            {
                Log($"CONNECT {protocol} {username}@{host}:{port}");
                if (username != server.Username || password != server.Password)
                    throw new UnauthorizedAccessException("Permission denied (fake server: bad credentials).");
                _connected = true;
            }
        }

        public void Upload(string localFile, string remoteFile, bool overwrite)
        {
            RequireConnected();
            byte[] content = File.ReadAllBytes(localFile);
            lock (server._gate)
            {
                string p = Normalize(remoteFile);
                Log($"UPLOAD {p}");
                if (!server._directories.Contains(ParentOf(p)))
                    throw new DirectoryNotFoundException($"No such directory: {ParentOf(p)}");
                if (server._directories.Contains(p))
                    throw new IOException($"A directory exists at {p}");
                if (server._files.ContainsKey(p) && !overwrite)
                    throw new IOException($"File already exists: {p}");
                server._files[p] = content;
            }
        }

        public void Download(string localFile, string remoteFile)
        {
            RequireConnected();
            byte[] content;
            lock (server._gate)
            {
                string p = Normalize(remoteFile);
                Log($"DOWNLOAD {p}");
                if (!server._files.TryGetValue(p, out byte[]? bytes))
                    throw new FileNotFoundException($"No such file: {p}");
                content = bytes;
            }
            File.WriteAllBytes(localFile, content);
        }

        public void RemoteCreateDirectory(string remotePath)
        {
            RequireConnected();
            lock (server._gate)
            {
                string p = Normalize(remotePath);
                Log($"MKDIR {p}");
                if (!server._directories.Contains(ParentOf(p)))
                    throw new DirectoryNotFoundException($"No such directory: {ParentOf(p)}");
                server._directories.Add(p);
            }
        }

        public bool RemoteFileExists(string remoteFile)
        {
            RequireConnected();
            lock (server._gate) return server._files.ContainsKey(Normalize(remoteFile));
        }

        public bool RemoteDirectoryExists(string remoteDirectory)
        {
            RequireConnected();
            lock (server._gate) return server._directories.Contains(Normalize(remoteDirectory));
        }

        public bool RemoteIsDirectory(string remotePath)
        {
            RequireConnected();
            lock (server._gate)
            {
                string p = Normalize(remotePath);
                if (server._directories.Contains(p)) return true;
                if (server._files.ContainsKey(p)) return false;
                throw new FileNotFoundException($"No such file or directory: {p}");
            }
        }

        public void RemoteFileDelete(string remoteFile)
        {
            RequireConnected();
            lock (server._gate)
            {
                string p = Normalize(remoteFile);
                Log($"RM {p}");
                if (!server._files.Remove(p))
                    throw new FileNotFoundException($"No such file: {p}");
            }
        }

        public void RemoteDirectoryDelete(string remoteDirectory)
        {
            RequireConnected();
            lock (server._gate)
            {
                string p = Normalize(remoteDirectory);
                Log($"RMDIR {p}");
                if (!server._directories.Contains(p) || p == "/")
                    throw new DirectoryNotFoundException($"No such directory: {p}");

                string prefix = p + "/";
                foreach (string file in server._files.Keys.Where(f => f.StartsWith(prefix, StringComparison.Ordinal)).ToList())
                    server._files.Remove(file);
                server._directories.RemoveWhere(d => d == p || d.StartsWith(prefix, StringComparison.Ordinal));
            }
        }

        // Local-disk operations: same behaviour as the real client, nothing to fake.
        public bool LocalFileExists(string localFile) => File.Exists(localFile);
        public bool LocalIsDirectory(string localPath) => Directory.Exists(localPath);

        public List<FtpSftpFileInfo> LocalListing(string localPath)
        {
            List<FtpSftpFileInfo> result = [];
            foreach (string file in Directory.GetFiles(localPath))
                result.Add(new FtpSftpFileInfo(Path.GetFileName(file), file, true, false, false));
            foreach (string dir in Directory.GetDirectories(localPath))
                result.Add(new FtpSftpFileInfo(Path.GetFileName(dir), dir, false, true, false));
            return result;
        }

        public void Dispose() { }
    }
}
