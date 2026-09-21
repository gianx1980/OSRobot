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
    void RemoteFileDelete(string remoteFile);
    void RemoteDirectoryDelete(string remoteDirectory);

    bool LocalFileExists(string localFile);
    bool LocalIsDirectory(string localPath);
    List<FtpSftpFileInfo> LocalListing(string localPath);
}
