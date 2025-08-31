using OSRobot.Server.Core;
using OSRobot.Server.Plugins.FileSystemEvent;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class TestFileSystemEvent
{
    [TestMethod]
    public void TestAddFile()
    {
        // ---------
        // Arrange
        // ---------
        int toleranceSec = 30;

        string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(appBasePath, @"TestFileEvent\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);

        FileSystemEventConfig config = new()
        {
            Id = 1,
            Name = "FileSystemEvent 1"
        };
        
        FolderToMonitor folderMon = new()
        {
            Path = testFileFolder,
            MonitorSubFolders = false,
            MonitorAction = MonitorActionType.NewFiles
        };
        config.FoldersToMonitor.Add(folderMon);

        FileSystemEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        object objSync = new();
        ManualResetEvent mre = new(false);
        bool eventTriggered = false;
        eventObj.EventTriggered += (sender, e) =>
        {
            lock (objSync)
            {
                eventTriggered = true;
            }

            mre.Set();
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        string filePath = Path.Combine(testFileFolder, "TestAdd.txt");
        using FileStream fs = new(filePath, FileMode.Create);
        using StreamWriter sw = new(fs);
        sw.WriteLine("This is a test!");
        
        mre.WaitOne(new TimeSpan(0, 0, toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered, "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestModifyFile()
    {
        // Arrange
        int toleranceSec = 30;
        string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(appBasePath, @"TestFileEvent\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);

        string filePath = Path.Combine(testFileFolder, "TestModify.txt");
        using (FileStream fs = new(filePath, FileMode.Create))
        {
            using (StreamWriter sw = new(fs))
            {
                sw.WriteLine("This is a test!");
            }
        }

        FileSystemEventConfig config = new()
        {
            Id = 1,
            Name = "FileSystemEvent 1"
        };
        
        FolderToMonitor folderMon = new()
        {
            Path = testFileFolder,
            MonitorSubFolders = false,
            MonitorAction = MonitorActionType.ModifiedFiles
        };

        config.FoldersToMonitor.Add(folderMon);

        FileSystemEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        object objSync = new();
        ManualResetEvent mre = new ManualResetEvent(false);
        bool eventTriggered = false;
        eventObj.EventTriggered += (sender, e) =>
        {
            lock (objSync)
            {
                eventTriggered = true;
            }

            mre.Set();
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        using (FileStream fs = new(filePath, FileMode.Append))
        {
            using (StreamWriter sw = new(fs))
            {
                sw.WriteLine("This is a test!");
            }
        }

        mre.WaitOne(new TimeSpan(0, 0, toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered, "The event did not occur at the expected time.");
        }
    }

    [TestMethod]
    public void TestDeleteFile()
    {
        // ---------
        // Arrange
        // ---------
        int toleranceSec = 30;
        string appBasePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(appBasePath, @"TestFileEvent\");
        Folder folder = Common.CreateRootFolder();

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);

        string filePath = Path.Combine(testFileFolder, "TestDelete.txt");
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine("This is a test!");
            }
        }

        FileSystemEventConfig config = new()
        {
            Id = 1,
            Name = "FileSystemEvent 1"
        };

        FolderToMonitor folderMon = new()
        {
            Path = testFileFolder,
            MonitorSubFolders = false,
            MonitorAction = MonitorActionType.DeletedFiles
        };        
        config.FoldersToMonitor.Add(folderMon);

        FileSystemEvent eventObj = new()
        {
            ParentFolder = folder,
            Config = config
        };

        object objSync = new();
        ManualResetEvent mre = new ManualResetEvent(false);
        bool eventTriggered = false;
        eventObj.EventTriggered += (sender, e) =>
        {
            lock (objSync)
            {
                eventTriggered = true;
            }

            mre.Set();
        };
        eventObj.Config = config;

        // ---------
        // Act
        // ---------
        eventObj.Init();

        File.Delete(filePath);

        mre.WaitOne(new TimeSpan(0, 0, toleranceSec));

        // ---------
        // Assert
        // ---------
        lock (objSync)
        {
            Assert.IsTrue(eventTriggered, "The event did not occur at the expected time.");
        }
    }
}
