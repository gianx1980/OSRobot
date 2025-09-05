using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.FtpSftpTask;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestFtpSftpTask
{
    [TestMethod]
    public void TestSftpCopyFolder()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestSftp1\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(testFileFolder + @"\SubFolder");

        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp1.txt"), "Test Sftp 1");
        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp2.txt"), "Test Sftp 2");
        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp3.txt"), "Test Sftp 3");
        Common.WriteTestFile(Path.Combine(testFileFolder + @"\SubFolder", "Sftp4.txt"), "Test Sftp 4");

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
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskUpload);

        dynDataChain.TryAdd(2, dynDataSet);

        // -------------------
        // Act && Assert
        // -------------------

        taskDelete.Init();
        ExecResult erTaskDelete = taskDelete.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskDelete.Destroy();
        Assert.IsTrue(erTaskDelete.Result, "TaskDelete failed.");

        taskUpload.Init();
        ExecResult erTaskUpload = taskUpload.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskUpload.Destroy();
        Assert.IsTrue(erTaskUpload.Result, "TaskUpload failed.");
    }

    [TestMethod]
    public void TestSftpCopyFile()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestSftp2\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(testFileFolder + @"\SubFolderSingle");

        Common.WriteTestFile(Path.Combine(testFileFolder + @"\SubFolderSingle", "SftpSingleFile.txt"), "Test Sftp Single file");

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
            LocalPath = Path.Combine(testFileFolder + @"\SubFolderSingle", "SftpSingleFile.txt"),
            RemotePath = @"\RemoteFolderSingle\SftpSingleFile.txt",
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
            RemotePath = @"\RemoteFolderSingle"
        };
        taskDeleteConfig.DeleteItems.Add(deleteItem);

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskUpload);

        dynDataChain.TryAdd(2, dynDataSet);

        // -------------------
        // Act && Assert
        // -------------------

        taskDelete.Init();    
        ExecResult erTaskDelete = taskDelete.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskDelete.Destroy();
        Assert.IsTrue(erTaskDelete.Result, "TaskDelete failed.");

        taskUpload.Init();
        ExecResult erTaskUpload = taskUpload.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskUpload.Destroy();
        Assert.IsTrue(erTaskUpload.Result, "TaskUpload failed.");
    }

    [TestMethod]
    public void TestFtpCopyFolder()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestFtp3\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(testFileFolder + @"\SubFolder");

        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp1.txt"), "Test Sftp 1");
        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp2.txt"), "Test Sftp 2");
        Common.WriteTestFile(Path.Combine(testFileFolder, "Sftp3.txt"), "Test Sftp 3");
        Common.WriteTestFile(Path.Combine(testFileFolder + @"\SubFolder", "Sftp4.txt"), "Test Sftp 4");

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
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(taskUpload);

        dynDataChain.TryAdd(2, dynDataSet);

        // ---------------
        // Act && Assert
        // ---------------
        taskDelete.Init();
        ExecResult erTaskDelete = taskDelete.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskDelete.Destroy();
        Assert.IsTrue(erTaskDelete.Result, "TaskDelete failed.");

        taskUpload.Init();
        ExecResult erTaskUpload = taskUpload.Run(dynDataChain, dynDataSet, 0, logger).ExecResults[0];
        taskUpload.Destroy();
        Assert.IsTrue(erTaskUpload.Result, "TaskUpload failed.");
    }
}
