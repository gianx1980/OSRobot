// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Plugins.FtpSftpTask;

/// <summary>
/// The operations FtpSftpTask needs from an FTP/SFTP connection. Exists so tests can substitute an
/// in-memory implementation instead of talking to a real server; the production implementation is
/// <see cref="FtpSftpClient"/>. Create instances through PluginServices.CreateFileTransferClient().
/// </summary>
public interface IFileTransferClient : IDisposable
{
    void Connect(ProtocolEnum protocol, string host, int port, string username, string password);

    void Upload(string localFile, string remoteFile, bool overwrite);
    void Download(string localFile, string remoteFile);

    void RemoteCreateDirectory(string remotePath);
    bool RemoteFileExists(string remoteFile);
    bool RemoteDirectoryExists(string remoteDirectory);
    bool RemoteIsDirectory(string remotePath);
    List<FtpSftpFileInfo> RemoteListing(string remotePath);
    void RemoteFileDelete(string remoteFile);
    void RemoteDirectoryDelete(string remoteDirectory);

    bool LocalFileExists(string localFile);
    bool LocalIsDirectory(string localPath);
    List<FtpSftpFileInfo> LocalListing(string localPath);
}
