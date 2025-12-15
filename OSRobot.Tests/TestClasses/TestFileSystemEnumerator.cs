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
