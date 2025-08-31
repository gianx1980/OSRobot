using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.FtpSftpTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestFtpSftpTask
{
    private void WriteTestFile(string filePathName, string content)
    {
        using FileStream fs = new(filePathName, FileMode.Create);
        using StreamWriter sw = new(fs);
        sw.WriteLine(content);   
    }

    [TestMethod]
    public void TestSftp()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestSftp\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(testFileFolder + @"\SubFolder");

        WriteTestFile(Path.Combine(testFileFolder, "Sftp1.txt"), "Test Sftp 1");
        WriteTestFile(Path.Combine(testFileFolder, "Sftp2.txt"), "Test Sftp 2");
        WriteTestFile(Path.Combine(testFileFolder, "Sftp3.txt"), "Test Sftp 3");
        WriteTestFile(Path.Combine(testFileFolder + @"\SubFolder", "Sftp4.txt"), "Test Sftp 4");

        FtpSftpTaskConfig taskUploadConfig = new()
        {
            Id = 1,
            Name = "Sftp Upload",
            Command = CommandEnum.Copy,
            Protocol = ProtocolEnum.SFTP,
            Host = "localhost",
            Username = "test",
            Password = "12345",
            Port = "22"
        };

        FtpSftpTask taskUpload = new()
        {
            Config = taskUploadConfig,
            ParentFolder = folder
        };

        FtpSftpCopyItem copyItem = new()
        {
            LocalToRemote = true,
            LocalPath = testFileFolder,
            RemotePath = @"\RemoteFolder",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = true
        };
        taskUploadConfig.CopyItems.Add(copyItem);


        FtpSftpTaskConfig taskDeleteConfig = new()
        {
            Id = 2,
            Name = "Sftp Delete",
            Command = CommandEnum.Delete,
            Protocol = ProtocolEnum.SFTP,
            Host = "localhost",
            Username = "test",
            Password = "12345",
            Port = "22"
        };

        FtpSftpTask taskDelete = new()
        {
            Config = taskDeleteConfig,
            ParentFolder = folder
        };

        FtpSftpDeleteItem deleteItem = new()
        {
            RemotePath = @"\RemoteFolder"
        };
        taskDeleteConfig.DeleteItems.Add(deleteItem);

        Common.ConfigureLogPath();
        (DynamicDataChain dataChain,
         DynamicDataSet dynamicDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskUpload);

        dataChain.TryAdd(2, dynamicDataSet);

        // -------------------
        // Act && Assert
        // -------------------

        ExecResult erTaskDelete = taskDelete.Run(dataChain, dynamicDataSet, 0, logger).ExecResults[0];
        Assert.IsTrue(erTaskDelete.Result, "TaskDelete failed.");

        ExecResult erTaskUpload = taskUpload.Run(dataChain, dynamicDataSet, 0, logger).ExecResults[0];
        Assert.IsTrue(erTaskUpload.Result, "TaskUpload failed.");
    }

    [TestMethod]
    public void TestFtp()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestFtp\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(testFileFolder + @"\SubFolder");

        WriteTestFile(Path.Combine(testFileFolder, "Sftp1.txt"), "Test Sftp 1");
        WriteTestFile(Path.Combine(testFileFolder, "Sftp2.txt"), "Test Sftp 2");
        WriteTestFile(Path.Combine(testFileFolder, "Sftp3.txt"), "Test Sftp 3");
        WriteTestFile(Path.Combine(testFileFolder + @"\SubFolder", "Sftp4.txt"), "Test Sftp 4");

        FtpSftpTaskConfig taskUploadConfig = new()
        {
            Id = 1,
            Name = "Ftp Upload",
            Command = CommandEnum.Copy,
            Protocol = ProtocolEnum.FTP,
            Host = "localhost",
            Username = "test",
            Password = "12345",
            Port = "21"
        };

        FtpSftpTask taskUpload = new()
        {
            Config = taskUploadConfig,
            ParentFolder = folder
        };

        FtpSftpCopyItem copyItem = new()
        {
            LocalToRemote = true,
            LocalPath = testFileFolder,
            RemotePath = @"\RemoteFolder",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = true
        };
        taskUploadConfig.CopyItems.Add(copyItem);

        FtpSftpTaskConfig taskDeleteConfig = new()
        {
            Id = 2,
            Name = "Ftp Delete",
            Command = CommandEnum.Delete,
            Protocol = ProtocolEnum.FTP,
            Host = "localhost",
            Username = "test",
            Password = "12345",
            Port = "21"
        };

        FtpSftpTask taskDelete = new()
        {
            Config = taskDeleteConfig,
            ParentFolder = folder
        };

        FtpSftpDeleteItem deleteItem = new()   
        {
            RemotePath = @"\RemoteFolder"
        };
        taskDeleteConfig.DeleteItems.Add(deleteItem);

        Common.ConfigureLogPath();
        (DynamicDataChain dataChain,
         DynamicDataSet dynamicDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskUpload);

        dataChain.TryAdd(2, dynamicDataSet);

        // ---------------
        // Act && Assert
        // ---------------
        ExecResult erTaskDelete = taskDelete.Run(dataChain, dynamicDataSet, 0, logger).ExecResults[0];
        Assert.IsTrue(erTaskDelete.Result, "TaskDelete failed.");

        ExecResult erTaskUpload = taskUpload.Run(dataChain, dynamicDataSet, 0, logger).ExecResults[0];
        Assert.IsTrue(erTaskUpload.Result, "TaskUpload failed.");
    }
}
