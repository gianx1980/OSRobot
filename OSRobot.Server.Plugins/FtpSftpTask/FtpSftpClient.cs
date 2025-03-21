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

using FluentFTP;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Net;

namespace OSRobot.Server.Plugins.FtpSftpTask;

public class FtpSftpClient : IDisposable
{
    private bool _disposed;
    private FtpClient? _ftpClient;
    private SftpClient? _sftpClient;
    private ProtocolEnum _protocol;

    public void Connect(ProtocolEnum protocol, string host, int port, string username, string password)
    {
        _protocol = protocol;
        if (_protocol == ProtocolEnum.FTP)
        {
            _ftpClient = new FtpClient(host, new NetworkCredential(username, password), port);
            _ftpClient.Connect();
        }
        else
        {
            _sftpClient = new SftpClient(host, port, username, password);
            _sftpClient.Connect();
        }
    }

    public void Disconnect()
    {
        if (_protocol == ProtocolEnum.FTP)
        {
            _ftpClient?.Disconnect();
            _ftpClient = null;
        }
        else
        {
            _sftpClient?.Disconnect();
            _sftpClient = null;
        }
    }

    public void Upload(string localFile, string remoteFile, bool overwrite)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP)
        {
            FtpRemoteExists existOption = overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
            _ftpClient?.UploadFile(localFile, remoteFile, existOption);
        }
        {
            using FileStream fStream = new(localFile, FileMode.Open);
            _sftpClient?.UploadFile(fStream, remoteFile, overwrite);
        }
    }

    public void Download(string localFile, string remoteFile)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP)
        {
            _ftpClient?.DownloadFile(localFile, remoteFile);
        }
        else
        {
            using FileStream fStream = new(localFile, FileMode.Create);
            _sftpClient?.DownloadFile(localFile, fStream);
        }
    }

    public void RemoteCreateDirectory(string remotePath)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP)
        {
            _ftpClient?.CreateDirectory(remotePath);
        }
        else
        {
            _sftpClient?.CreateDirectory(remotePath);
        }
    }

    public void LocalCreateDirectory(string localPath)
    {
        Directory.CreateDirectory(localPath);
    }

    public bool RemoteFileExists(string remoteFile)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP && _ftpClient != null)
            return _ftpClient.FileExists(remoteFile);
        else if (_sftpClient != null)
            return _sftpClient.Exists(remoteFile);

        return false;
    }

    public bool LocalFileExists(string localFile)
    {
        return File.Exists(localFile);
    }

    public bool RemoteDirectoryExists(string remoteDirectory)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP && _ftpClient != null)
            return _ftpClient.DirectoryExists(remoteDirectory);
        else if (_sftpClient != null)
            return _sftpClient.Exists(remoteDirectory);

        return false;
    }

    public bool LocalDirectoryExists(string localDirectory)
    {
        return Directory.Exists(localDirectory);
    }

    public void LocalFileDelete(string localFile)
    {
        File.Delete(localFile);
    }

    public void RemoteFileDelete(string remoteFile)
    {
        if (_ftpClient != null && _sftpClient != null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_protocol == ProtocolEnum.FTP)
            _ftpClient?.DeleteFile(remoteFile);
        else if (_sftpClient != null)
            _sftpClient?.Delete(remoteFile);
    }

    public void LocalDirectoryDelete(string localDirectory)
    {
        Directory.Delete(localDirectory);
    }

    private void SFtpDeleteDirectoryRecursive(SftpClient client, string path)
    {
        foreach (SftpFile file in client.ListDirectory(path).Cast<SftpFile>())
        {
            if ((file.Name != ".") && (file.Name != ".."))
            {
                if (file.IsDirectory)
                {
                    SFtpDeleteDirectoryRecursive(client, file.FullName);
                }
                else
                {
                    client.DeleteFile(file.FullName);
                }
            }
        }

        client.DeleteDirectory(path);
    }

    public void RemoteDirectoryDelete(string remoteDirectory)
    {
        if (_ftpClient != null && _sftpClient != null)
            throw new ApplicationException("Method \"Connect\" not called.");

        if (_ftpClient != null)
            _ftpClient.DeleteDirectory(remoteDirectory, FtpListOption.Recursive);
        else if (_sftpClient != null)
        {
            SFtpDeleteDirectoryRecursive(_sftpClient, remoteDirectory);
        }
    }

    public List<FtpSftpFileInfo> RemoteListing(string remotePath)
    {
        if (_ftpClient == null && _sftpClient == null)
            throw new ApplicationException("Method \"Connect\" not called.");

        List<FtpSftpFileInfo> result = [];

        if (_ftpClient != null)
        {
            FtpListItem[] list = _ftpClient.GetListing(remotePath);
            foreach (FtpListItem item in list)
            {
                FtpSftpFileInfo fileInfo = new(item.Name, item.FullName,
                                                    item.Type == FtpObjectType.File,
                                                    item.Type == FtpObjectType.Directory,
                                                    item.Type == FtpObjectType.Link);
                result.Add(fileInfo);
            }
        }
        else if (_sftpClient != null)
        {
            IEnumerable<ISftpFile> list = _sftpClient.ListDirectory(remotePath);
            foreach (ISftpFile item in list)
            {
                FtpSftpFileInfo fileInfo = new(item.Name, item.FullName,
                                                                item.IsRegularFile,
                                                                item.IsDirectory,
                                                                item.IsSymbolicLink);
                result.Add(fileInfo);
            }
        }

        return result;
    }

    public List<FtpSftpFileInfo> LocalListing(string localPath)
    {
        List<FtpSftpFileInfo> result = [];

        string[] files = Directory.GetFiles(localPath);
        foreach (string fullPathFileName in files)
        {
            result.Add(new FtpSftpFileInfo(Path.GetFileName(fullPathFileName), fullPathFileName, true, false, false));
        }

        string[] directories = Directory.GetDirectories(localPath);
        foreach (string fullPathDirectoryName in directories)
        {
            
            List<string> pathItems = FtpSftpTaskCommon.SplitLocalPath(fullPathDirectoryName);
            string directoryName = pathItems[^1];
            result.Add(new FtpSftpFileInfo(directoryName, fullPathDirectoryName, false, true, false));
        }

        return result;
    }

    public bool RemoteIsDirectory(string remotePath)
    {
        if (_ftpClient != null)
        {
            FtpListItem Info = _ftpClient.GetObjectInfo(remotePath);
            return (Info.Type == FtpObjectType.Directory);
        }
        else if (_sftpClient != null)
        {
            ISftpFile Info = _sftpClient.Get(remotePath);
            return (Info.IsDirectory);
        }

        throw new ApplicationException("Method \"Connect\" not called.");
    }

    public bool LocalIsDirectory(string localPath)
    {
        return Common.IsDirectory(localPath);
    }


    public void Dispose()
    {
        Dispose(true);
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        /*
        if (disposing)
        {
            
        }
        */

        try { Disconnect(); } finally { }

        _disposed = true;
    }
}
