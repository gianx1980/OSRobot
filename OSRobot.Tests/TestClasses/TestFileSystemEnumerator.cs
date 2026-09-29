// SPDX-FileCopyrightText: 2025 Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Plugins.Infrastructure.Utilities.FileSystem;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestFileSystemEnumerator
{
    [TestMethod]
    public void Test()
    {
        // ---------
        // Arrange
        // ---------
        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string testFileFolder = Path.Combine(basePath, @"TestFileSystemEnumerator\");

        if (Directory.Exists(testFileFolder))
            Directory.Delete(testFileFolder, true);
        Directory.CreateDirectory(testFileFolder);
        Directory.CreateDirectory(Path.Combine(testFileFolder, "SubFolder"));
        Directory.CreateDirectory(Path.Combine(testFileFolder, "SubFolder", "SubSubFolder"));

        string[] filesToCreate =
        [
            Path.Combine(testFileFolder, "TestFileEnumerator1.txt"),
            Path.Combine(testFileFolder, "TestFileEnumerator2.txt"),
            Path.Combine(testFileFolder, "TestFileEnumerator3.txt"),
            Path.Combine(testFileFolder, "SubFolder", "TestFileEnumerator4.txt"),
            Path.Combine(testFileFolder, "SubFolder", "TestFileEnumerator5.txt"),
            Path.Combine(testFileFolder, "SubFolder", "TestFileEnumerator6.txt"),
            Path.Combine(testFileFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator7.txt"),
            Path.Combine(testFileFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator8.txt"),
            Path.Combine(testFileFolder, "SubFolder", "SubSubFolder", "TestFileEnumerator9.txt"),
        ];

        foreach (string file in filesToCreate)
        {
            Common.WriteTestFile(file, "Test content");
        }


        // ---------
        // Act
        // ---------
        List<string> foundFiles = new();    

        FileSystemEnumerator fileSystemEnumerator = new(testFileFolder, true);
        foreach (string file in fileSystemEnumerator)
        {
            foundFiles.Add(file);   
        }

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(foundFiles.Count == filesToCreate.Length, "File count mismatch");

        foreach (string file in filesToCreate)
        {
           Assert.IsTrue(foundFiles.Contains(file), $"File not found: {file}");
        }
    }
}
