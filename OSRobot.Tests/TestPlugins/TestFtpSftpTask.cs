using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.FtpSftpTask;
using OSRobot.Server.Plugins.Infrastructure.Network;
using OSRobot.Tests.Fakes;

namespace OSRobot.Tests.TestPlugins;

/// <summary>
/// FtpSftpTask tests against an in-memory FTP/SFTP server (see InMemoryFileTransferServer): no real
/// server, port or credentials involved. The real-server versions live in IntegrationTests.
/// </summary>
[TestClass]
public sealed class TestFtpSftpTask
{
    private string _localRoot = null!;
    private InMemoryFileTransferServer _server = null!;
    private IDisposable _serviceScope = null!;

    [TestInitialize]
    public void Setup()
    {
        _localRoot = Path.Combine(Path.GetTempPath(), "OSRobotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_localRoot);

        _server = new InMemoryFileTransferServer();
        _serviceScope = PluginServices.Override(fileTransferClientFactory: _server.CreateClient);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceScope.Dispose();
        if (Directory.Exists(_localRoot))
            Directory.Delete(_localRoot, true);
    }

    private string CreateLocalFile(string relativePath, string content)
    {
        string path = Path.Combine(_localRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static FtpSftpTaskConfig NewConfig(ProtocolEnum protocol, CommandEnum command, string password = "12345") => new()
    {
        Id = 1,
        Name = "Ftp/Sftp task",
        Command = command,
        Protocol = protocol,
        Host = "localhost",
        Port = protocol == ProtocolEnum.SFTP ? "22" : "21",
        Username = "test",
        Password = password
    };

    private static async Task<ExecResult> RunAsync(FtpSftpTaskConfig config)
    {
        FtpSftpTask task = new() { Config = config, ParentFolder = Common.CreateRootFolder() };

        Common.ConfigureLogPath();
        (DynamicDataChain chain, DynamicDataSet dataSet, IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        task.Init();
        try
        {
            return (await task.RunAsync(chain, dataSet, 0, logger, CancellationToken.None)).ExecResults[0];
        }
        finally
        {
            task.Destroy();
        }
    }

    [TestMethod]
    [DataRow(ProtocolEnum.SFTP)]
    [DataRow(ProtocolEnum.FTP)]
    public async Task UploadFolder_copies_the_whole_tree(ProtocolEnum protocol)
    {
        CreateLocalFile(@"src\Sftp1.txt", "one");
        CreateLocalFile(@"src\Sftp2.txt", "two");
        CreateLocalFile(@"src\SubFolder\Sftp3.txt", "three");

        FtpSftpTaskConfig config = NewConfig(protocol, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = Path.Combine(_localRoot, "src"),
            RemotePath = @"\RemoteFolder",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Upload failed.");
        CollectionAssert.AreEqual(
            new[] { "/RemoteFolder/Sftp1.txt", "/RemoteFolder/Sftp2.txt", "/RemoteFolder/SubFolder/Sftp3.txt" },
            _server.AllFiles());
        Assert.AreEqual("three", _server.ReadAllText("/RemoteFolder/SubFolder/Sftp3.txt"));
    }

    [TestMethod]
    public async Task UploadFolder_without_recursion_skips_subfolders()
    {
        CreateLocalFile(@"src\top.txt", "top");
        CreateLocalFile(@"src\SubFolder\nested.txt", "nested");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = Path.Combine(_localRoot, "src"),
            RemotePath = "/RemoteFolder",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = false
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Upload failed.");
        CollectionAssert.AreEqual(new[] { "/RemoteFolder/top.txt" }, _server.AllFiles());
    }

    [TestMethod]
    public async Task UploadFile_creates_the_missing_remote_directories()
    {
        string local = CreateLocalFile(@"one\file.txt", "single");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = local,
            RemotePath = @"\A\B\file.txt",
            OverwriteFileIfExists = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Upload failed.");
        Assert.IsTrue(_server.DirectoryExists("/A/B"));
        Assert.AreEqual("single", _server.ReadAllText("/A/B/file.txt"));
    }

    [TestMethod]
    public async Task UploadFile_keeps_the_remote_file_when_overwrite_is_off()
    {
        string local = CreateLocalFile("file.txt", "new content");
        _server.SeedFile("/Remote/file.txt", "old content");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = local,
            RemotePath = "/Remote/file.txt",
            OverwriteFileIfExists = false
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Task failed.");
        Assert.AreEqual("old content", _server.ReadAllText("/Remote/file.txt"));
        Assert.IsFalse(_server.Operations.Any(o => o.StartsWith("UPLOAD")), "Nothing should have been uploaded.");
    }

    [TestMethod]
    public async Task UploadFile_overwrites_the_remote_file_when_overwrite_is_on()
    {
        string local = CreateLocalFile("file.txt", "new content");
        _server.SeedFile("/Remote/file.txt", "old content");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = local,
            RemotePath = "/Remote/file.txt",
            OverwriteFileIfExists = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Task failed.");
        Assert.AreEqual("new content", _server.ReadAllText("/Remote/file.txt"));
    }

    [TestMethod]
    public async Task Delete_removes_a_remote_directory_and_everything_in_it()
    {
        _server.SeedFile("/RemoteFolder/a.txt", "a");
        _server.SeedFile("/RemoteFolder/Sub/b.txt", "b");
        _server.SeedFile("/Other/keep.txt", "keep");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Delete);
        config.DeleteItems.Add(new FtpSftpDeleteItem { RemotePath = @"\RemoteFolder" });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Delete failed.");
        Assert.IsFalse(_server.DirectoryExists("/RemoteFolder"));
        CollectionAssert.AreEqual(new[] { "/Other/keep.txt" }, _server.AllFiles());
    }

    [TestMethod]
    public async Task Delete_removes_a_single_remote_file()
    {
        _server.SeedFile("/RemoteFolder/a.txt", "a");
        _server.SeedFile("/RemoteFolder/b.txt", "b");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Delete);
        config.DeleteItems.Add(new FtpSftpDeleteItem { RemotePath = "/RemoteFolder/a.txt" });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Delete failed.");
        CollectionAssert.AreEqual(new[] { "/RemoteFolder/b.txt" }, _server.AllFiles());
    }

    [TestMethod]
    public async Task Delete_of_a_missing_path_succeeds_and_changes_nothing()
    {
        _server.SeedFile("/RemoteFolder/a.txt", "a");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Delete);
        config.DeleteItems.Add(new FtpSftpDeleteItem { RemotePath = "/DoesNotExist" });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Delete of a missing path should not fail.");
        CollectionAssert.AreEqual(new[] { "/RemoteFolder/a.txt" }, _server.AllFiles());
    }

    [TestMethod]
    public async Task Wrong_credentials_fail_the_task_before_touching_anything()
    {
        string local = CreateLocalFile("file.txt", "secret");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy, password: "wrong");
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = true,
            LocalPath = local,
            RemotePath = "/Remote/file.txt",
            OverwriteFileIfExists = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsFalse(result.Result, "The task should fail on authentication.");
        Assert.HasCount(1, _server.Operations, "Only the CONNECT attempt is expected.");
        Assert.IsEmpty(_server.AllFiles());
    }

    [TestMethod]
    public async Task Connection_uses_the_configured_protocol_host_port_and_user()
    {
        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.FTP, CommandEnum.Delete);
        config.Host = "files.example.com";
        config.Port = "2121";

        await RunAsync(config);

        Assert.AreEqual("CONNECT FTP test@files.example.com:2121", _server.Operations[0]);
    }

    [TestMethod]
    [DataRow(ProtocolEnum.SFTP)]
    [DataRow(ProtocolEnum.FTP)]
    public async Task DownloadFile_copies_the_remote_file_and_creates_the_local_directories(ProtocolEnum protocol)
    {
        _server.SeedFile("/Remote/file.txt", "downloaded");
        string localTarget = Path.Combine(_localRoot, "out", "deeper", "file.txt");

        FtpSftpTaskConfig config = NewConfig(protocol, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = false,
            LocalPath = localTarget,
            RemotePath = "/Remote/file.txt",
            OverwriteFileIfExists = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Download failed.");
        Assert.AreEqual("downloaded", File.ReadAllText(localTarget));
        Assert.IsFalse(_server.Operations.Any(o => o.StartsWith("MKDIR")), "Local directories must be created locally, not on the server.");
    }

    [TestMethod]
    public async Task DownloadFile_keeps_the_local_file_when_overwrite_is_off()
    {
        _server.SeedFile("/Remote/file.txt", "remote content");
        string local = CreateLocalFile("file.txt", "local content");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = false,
            LocalPath = local,
            RemotePath = "/Remote/file.txt",
            OverwriteFileIfExists = false
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Task failed.");
        Assert.AreEqual("local content", File.ReadAllText(local));
        Assert.IsFalse(_server.Operations.Any(o => o.StartsWith("DOWNLOAD")), "Nothing should have been downloaded.");
    }

    [TestMethod]
    [DataRow(ProtocolEnum.SFTP)]
    [DataRow(ProtocolEnum.FTP)]
    public async Task DownloadFolder_copies_the_whole_remote_tree(ProtocolEnum protocol)
    {
        _server.SeedFile("/Remote/a.txt", "a");
        _server.SeedFile("/Remote/b.txt", "b");
        _server.SeedFile("/Remote/Sub/c.txt", "c");
        _server.SeedFile("/Remote/Sub/Deep/d.txt", "d");
        _server.SeedFile("/Elsewhere/not-copied.txt", "x");
        string target = Path.Combine(_localRoot, "downloaded");

        FtpSftpTaskConfig config = NewConfig(protocol, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = false,
            LocalPath = target,
            RemotePath = "/Remote",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = true
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Download failed.");
        Assert.AreEqual("a", File.ReadAllText(Path.Combine(target, "a.txt")));
        Assert.AreEqual("b", File.ReadAllText(Path.Combine(target, "b.txt")));
        Assert.AreEqual("c", File.ReadAllText(Path.Combine(target, "Sub", "c.txt")));
        Assert.AreEqual("d", File.ReadAllText(Path.Combine(target, "Sub", "Deep", "d.txt")));
        Assert.AreEqual(4, Directory.GetFiles(target, "*", SearchOption.AllDirectories).Length, "Only the remote folder's files should arrive.");
    }

    [TestMethod]
    public async Task DownloadFolder_without_recursion_skips_subfolders()
    {
        _server.SeedFile("/Remote/top.txt", "top");
        _server.SeedFile("/Remote/Sub/nested.txt", "nested");
        string target = Path.Combine(_localRoot, "downloaded");

        FtpSftpTaskConfig config = NewConfig(ProtocolEnum.SFTP, CommandEnum.Copy);
        config.CopyItems.Add(new FtpSftpCopyItem
        {
            LocalToRemote = false,
            LocalPath = target,
            RemotePath = "/Remote",
            OverwriteFileIfExists = true,
            RecursivelyCopyDirectories = false
        });

        ExecResult result = await RunAsync(config);

        Assert.IsTrue(result.Result, "Download failed.");
        CollectionAssert.AreEqual(new[] { Path.Combine(target, "top.txt") }, Directory.GetFiles(target, "*", SearchOption.AllDirectories));
    }
}
