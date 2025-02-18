//using Newtonsoft.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using OSRobot.Server.Configuration;
using OSRobot.Server.Core;
using OSRobot.Server.Core.Logging;
using OSRobot.Server.Core.Persistence;
using OSRobot.Server.JobEngineLib;
using OSRobot.Server.Plugins.DateTimeEvent;
using OSRobot.Server.Plugins.WriteTextFileTask;
using Renci.SshNet;
using Serilog;
using System.Collections;
using System.Data;
using System.Xml;


/*
string? directory1 = Path.GetDirectoryName("D:\\_Appoggio\\TestJE\\test.txt");
string? fileName1 = Path.GetFileName("D:\\_Appoggio\\TestJE\\test.txt");
string[] files1 = Directory.GetFiles("D:\\_Appoggio\\TestJE\\");
string? t = Path.GetDirectoryName("D:\\");
DateTime dt1 = File.GetLastWriteTime("D:\\_Appoggio\\TestJE\\test.txt");


string? directory2 = Path.GetDirectoryName("D:\\_Appoggio\\TestJE\\*.*");
string? fileName2 = Path.GetFileName("D:\\_Appoggio\\TestJE\\*.*");
//string[] files2 = Directory.GetFiles("D:\\_Appoggio\\TestJE\\*.*");
//DateTime dt2 = File.GetLastWriteTime(directory2!);

string? directory3 = Path.GetDirectoryName("D:\\_Appoggio\\TestJE");
string? fileName3 = Path.GetFileName("D:\\_Appoggio\\TestJE\\TestJE");
string[] files3 = Directory.GetFiles("D:\\_Appoggio\\TestJE\\");
DateTime dt3 = File.GetLastWriteTime("D:\\_Appoggio\\TestJE");
DateTime dt4 = Directory.GetLastWriteTime("D:\\_Appoggio\\TestJE");

string? directory4 = Path.GetDirectoryName("D:\\_Appoggio\\TestJE\\");
DateTime dt5 = File.GetLastWriteTime("D:\\_Appoggio\\TestJE\\");

Console.WriteLine("XXXX");
*/


bool IsNameAPattern(string objectName) => objectName.Contains("*") || objectName.Contains("?");

void Copy(string source, string destination, bool overwriteExistingFiles, bool recursive = false, TimeSpan? olderThan = null)
{
    // Check input parameters
    if (string.IsNullOrEmpty(source))
        throw new ApplicationException("Copy: Source is empty.");

    if (string.IsNullOrEmpty(destination))
        throw new ApplicationException("Copy: Destination is empty.");

    string? sourcePath = Path.GetDirectoryName(source);
    if (string.IsNullOrEmpty(sourcePath))
        throw new ApplicationException($"Copy: Source {source} must contain path information.");

    string? destinationPath = Path.GetDirectoryName(destination);
    if (string.IsNullOrEmpty(destination))
        throw new ApplicationException($"Copy: Destination {destination} must contain path information.");

    string sourceName = Path.GetFileName(source);

    bool isSourceANamePattern = IsNameAPattern(sourceName);
    bool isSourceADirectory = Directory.Exists(source);

    // ------------------------------------------------------------------
    // Single file copy.
    // If destination is a folder, then use the same filename of source
    // as the destination file name.
    // ------------------------------------------------------------------
    if (!isSourceADirectory && !isSourceANamePattern)
    {
        if (Directory.Exists(destination))
            destination = Path.Combine(destination, sourceName);

        File.Copy(source, destination);
        return;
    }

    // ------------------------------------------------------------------
    // Folder copy or folder copy with a file name pattern
    // ------------------------------------------------------------------
    string? searchPattern;
    DirectoryInfo sourceDirInfo;
    if (isSourceADirectory)
    {
        searchPattern = "*";
        sourceDirInfo = new DirectoryInfo(source);
    }
    else
    {
        searchPattern = sourceName;
        sourcePath = Path.GetDirectoryName(source);
        sourceDirInfo = new DirectoryInfo(sourcePath!);
    }

    // Cache directories before we start copying
    DirectoryInfo[] subDirectories = sourceDirInfo.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destination);

    // Get the files in the source directory and copy to the destination directory
    foreach (FileInfo file in sourceDirInfo.GetFiles(searchPattern))
    {
        // Check file time if needed
        if (olderThan != null && file.LastWriteTime >= DateTime.Now.Subtract((TimeSpan)olderThan))
            continue;

        string targetFilePath = Path.Combine(destination, file.Name);

        if (!overwriteExistingFiles && File.Exists(targetFilePath))
            continue;

        file.CopyTo(targetFilePath, overwriteExistingFiles);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in subDirectories)
        {
            string newDestinationDir = Path.Combine(destination, subDir.Name);
            Copy(Path.Combine(subDir.FullName, searchPattern), newDestinationDir, overwriteExistingFiles, true, olderThan);
        }
    }
}


void Delete(string source, bool recursive = false, TimeSpan? olderThan = null)
{
    // Check input parameters
    if (string.IsNullOrEmpty(source))
        throw new ApplicationException("Copy: Source is empty.");

    string? sourcePath = Path.GetDirectoryName(source);
    if (string.IsNullOrEmpty(sourcePath))
        throw new ApplicationException($"Copy: Source {source} must contain path information.");

    string sourceName = Path.GetFileName(source);

    bool isSourceANamePattern = IsNameAPattern(sourceName);
    bool isSourceADirectory = Directory.Exists(source);


    // ------------------------------------------------------------------
    // Single file delete.
    // ------------------------------------------------------------------
    if (!isSourceADirectory && !isSourceANamePattern)
    {
        File.Delete(source);
        return;
    }

    // ------------------------------------------------------------------
    // Folder delete or folder delete with a file name pattern
    // ------------------------------------------------------------------
    
    // If source contains a pattern, all the files that respect the pattern
    // will be deleted, but containing empty folders will not be delete.
    // If source contains a directory name, then the directory and its content
    // will be completely deleted.


    string? searchPattern;
    DirectoryInfo sourceDirInfo;
    if (isSourceADirectory)
    {
        searchPattern = "*";
        sourceDirInfo = new DirectoryInfo(source);
    }
    else
    {
        searchPattern = sourceName;
        sourcePath = Path.GetDirectoryName(source);
        sourceDirInfo = new DirectoryInfo(sourcePath!);
    }

    // Cache directories before we start copying
    DirectoryInfo[] subDirectories = sourceDirInfo.GetDirectories();

    // Get the files in the source directory and delete
    foreach (FileInfo file in sourceDirInfo.GetFiles(searchPattern))
    {
        // Check file time if needed
        if (olderThan != null && file.LastWriteTime >= DateTime.Now.Subtract((TimeSpan)olderThan))
            continue;

        // Don't throw exception if file not exists
        if (file.Exists)
            file.Delete();
    }

    // If recursive delete subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in subDirectories)
        {
            string tempDirPathName = isSourceADirectory ? subDir.FullName : Path.Combine(subDir.FullName, searchPattern);
            Delete(tempDirPathName, true, olderThan);
        }
    }

    // If directory is empty, delete
    if (isSourceADirectory && sourceDirInfo.GetFiles().Length == 0)
        sourceDirInfo.Delete();
}


Delete("D:\\_Appoggio\\OSRobot\\ReleaseTempFolder\\_RuntimeFolders", true, null);


/*
Copy("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test.txt", true, true, null);
Delete("D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test.txt", true, null);

Copy("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test2.txt", true, true, null);
Delete("D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test2.txt", true, null);

Copy("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\", true, true, null);
Delete("D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test.txt", true, null);

Copy("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, true, null);
Delete("D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, null);
*/


//Copy("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile", true, null);

// Test directory copy
// D:\_Appoggio\TestJE
//Copy("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, null);
//Copy("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory\\", true, null);

// Test pattern
//Copy("D:\\_Appoggio\\TestJE\\*.txt", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, true, new TimeSpan(30, 0, 0, 0));

//CopyFileSystemObject("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\DestCopy", true, null);




/*
bool ObjectNameIsPattern(string objectName) => objectName.Contains("*") || objectName.Contains("?");

bool IsDirectory(string path)
{
    FileAttributes Attr = File.GetAttributes(path);
    return Attr.HasFlag(FileAttributes.Directory);
}


void CopyDirectory(string source, string destination, bool recursive)
{
    // Get information about the source directory
    var dir = new DirectoryInfo(source);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destination);

    // Get the files in the source directory and copy to the destination directory
    foreach (FileInfo file in dir.GetFiles())
    {
        string targetFilePath = Path.Combine(destination, file.Name);
        file.CopyTo(targetFilePath);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
}

void CopyFileSystemObject(string source, string destination, bool overWriteFileIfExists, TimeSpan? fileTimeThreshold)
{
    // Check input parameters
    if (string.IsNullOrEmpty(source))
        throw new ApplicationException("CopyFileSystemObject: Source is empty.");

    if (string.IsNullOrEmpty(destination))
        throw new ApplicationException("CopyFileSystemObject: Destination is empty.");

    string? sourceObjectPath = Path.GetDirectoryName(source);
    if (string.IsNullOrEmpty(sourceObjectPath))
        throw new ApplicationException($"CopyFileSystemObject: Source {source} must contain path information.");

    string? destinationObjectPath = Path.GetDirectoryName(destination);
    if (string.IsNullOrEmpty(destination))
        throw new ApplicationException($"CopyFileSystemObject: Destination {destination} must contain path information.");


    string sourceObjectName = Path.GetFileName(source);
    string destinationObjectName = Path.GetFileName(destination);

    // TODO: correct path of destination?
    if (ObjectNameIsPattern(sourceObjectName))
    {
        // A pattern
        string[]? objectList = Directory.GetFiles(sourceObjectPath, sourceObjectName, SearchOption.AllDirectories);
        if (objectList == null)
            throw new ApplicationException($"CopyFileSystemObject: Error getting content from {sourceObjectPath}");

        foreach (string objectToCopy in objectList)
        {
            if (IsDirectory(objectToCopy))
            {
                CopyDirectory(source, destination, true);
            }
            else
            {
                File.Copy(objectToCopy, destination, overWriteFileIfExists);
            }
        }
    }
    else if (IsDirectory(source))
    {
        // A Directory
        CopyDirectory(source, destination, true);
    }
    else
    {
        // A file

        // If destination object exists, check if is a directory.
        // If so, the file must be copied into that directory.
        if (Directory.Exists(destination))
        {
            destinationObjectName = string.Empty;
        }

        // If destinationObjectName is empty, use sourceObjectName as destination file name
        if (string.IsNullOrEmpty(destinationObjectName))
        {
            destination = Path.Combine(destination, sourceObjectName);
        }

        File.Copy(source, destination, overWriteFileIfExists);
    }
}



// Test file copy
CopyFileSystemObject("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test.txt", true, null);
CopyFileSystemObject("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\Test2.txt", true, null);
CopyFileSystemObject("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile\\", true, null);
CopyFileSystemObject("D:\\_Appoggio\\TestJE\\Test.txt", "D:\\_Appoggio\\TestCopy\\TestCopyFile", true, null);

// Test directory copy
// D:\_Appoggio\TestJE
CopyFileSystemObject("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, null);
CopyFileSystemObject("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory\\", true, null);

// Test pattern
CopyFileSystemObject("D:\\_Appoggio\\TestJE\\*.txt", "D:\\_Appoggio\\TestCopy\\TestCopyDirectory", true, null);

//CopyFileSystemObject("D:\\_Appoggio\\TestJE", "D:\\_Appoggio\\DestCopy", true, null);
*/


/*
    Func CopiaFileSystemObject

    Pattern
    File
    Directory

    If path = pattern
        Ricava il path della directory
        ListaFileDir = [Elenco dei file e directory di quella directory con quel pattern]
    Else if path = file
        ListaFileDir = [file]
    Else if path = directory
        ListaFileDir = [Elenco dei file e directory di quella directory]

    ForEach Item in ListaFileDir
        If Item is file
            Copia file  
        Else If Item is directory
            CopiaFileSystemObject(Item)
    Next   

 */






/*
Dictionary<string, string?> D1 = new Dictionary<string, string?>();
D1.Add("k1", "v1");
D1.Add("k2", "v2");
D1.Add("k3", "v3");
D1.Add("k4", null);

IDictionary DI = D1;

foreach (DictionaryEntry k in DI)
{
    System.Diagnostics.Debug.WriteLine(k.Key);
    System.Diagnostics.Debug.WriteLine(k.Value);
}

XmlSerialization serialization = new XmlSerialization();
string S = serialization.SerializeToXmlString(D1, "OsRobot");


System.Diagnostics.Debug.WriteLine(S);

XmlDocument xml = new XmlDocument();
xml.LoadXml(S);

XmlDeserialization deserialization = new XmlDeserialization(xml);
object? o = deserialization.Deserialize();

System.Diagnostics.Debug.WriteLine(o);
*/

/*
DataTable Dt = new DataTable();
Dt.TableName = "TableName";
Dt.Columns.Add(new DataColumn("Test1", typeof(string)));
Dt.Columns.Add(new DataColumn("Test2", typeof(string)));
Dt.Columns.Add(new DataColumn("Test3", typeof(string)));

DataRow DR = Dt.NewRow();
DR["Test1"] = "Test 123 Test 123";
DR["Test2"] = "Test 123 Test 123";
DR["Test3"] = "Test 123 Test 123";
Dt.Rows.Add(DR);

DR = Dt.NewRow();
DR["Test1"] = "Row 1 Test 123 Test 123";
DR["Test2"] = "Row 2 Test 123 Test 123";
DR["Test3"] = "Row 3 Test 123 Test 123";
Dt.Rows.Add(DR);


XmlSerialization serialization = new XmlSerialization();
string S = serialization.SerializeToXmlString(Dt, "OsRobot");

System.Diagnostics.Debug.WriteLine(S);

XmlDocument xml = new XmlDocument();
xml.LoadXml(S);

XmlDeserialization deserialization = new XmlDeserialization(xml);
object? obj = deserialization.Deserialize();

System.Diagnostics.Debug.WriteLine("XXXX");
*/
/*
MemoryStream MS = new MemoryStream();
Dt.WriteXml(MS, true);
MS.Seek(0, SeekOrigin.Begin);
StreamReader SR = new StreamReader(MS); 
string str = SR.ReadToEnd();
System.Diagnostics.Debug.WriteLine(str);
*/


/*
Folder rootFolder = new Folder();
rootFolder.Config = new FolderConfig();
rootFolder.Config.Id = 1;
rootFolder.Config.Name = "Root folder";
rootFolder.Config.Enabled = true;
rootFolder.Config.Log = true;


DateTimeEvent dtEvent1 = new DateTimeEvent();
DateTimeEventConfig dtEventConfig = new DateTimeEventConfig();
dtEventConfig.Id = 2;
dtEventConfig.Name = "Date time event 1";
dtEventConfig.AtDate = DateTime.Now.AddMinutes(1);

dtEvent1.Config = dtEventConfig;
dtEvent1.ParentFolder = rootFolder;


WriteTextFileTask task1 = new WriteTextFileTask();
WriteTextFileTaskConfig cfgTask1 = new WriteTextFileTaskConfig();
cfgTask1.Id = 5;
cfgTask1.Name = "Write text file task 1";
cfgTask1.FilePath = "D:\\_Appoggio\\TestJE";
cfgTask1.FormatAsDelimitedFile = true;
cfgTask1.DelimiterTab = true;

cfgTask1.ColumnsDefinition.Add(new WriteTextFileColumnDefinition("Column1", "Value1", "10"));
cfgTask1.ColumnsDefinition.Add(new WriteTextFileColumnDefinition("Column2", "Value2", "10"));

task1.Config = cfgTask1;
task1.ParentFolder = rootFolder;

rootFolder.Add(dtEvent1);
rootFolder.Add(task1);

List<ExecutionCondition> executionConditions = new List<ExecutionCondition>();
executionConditions.Add(new ExecutionCondition(dtEvent1, string.Empty, EnumExecutionConditionOperator.ObjectExecutes, string.Empty, string.Empty));
List<ExecutionCondition> dontExecutionConditions = new List<ExecutionCondition>();

dtEvent1.Connections.Add(new PluginInstanceConnection(task1, false, 10, executionConditions, dontExecutionConditions));

JobsPersistence.SaveXML("D:\\_Appoggio\\OSRobot\\Data\\", "OSRobotJobs.xml", rootFolder);

return;
*/

/*
Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("D:\\_Appoggio\\OSRobot\\Logs\\OSRobotEngine.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

ILogger logger = Log.Logger;

AppSettings appSettings = new AppSettings();
appSettings.JobEngineConfig.DataPath = "D:\\_Appoggio\\OSRobot\\Data\\";
appSettings.JobEngineConfig.LogPath = "D:\\_Appoggio\\OSRobot\\Logs\\";
appSettings.JobEngineConfig.SerialExecution = false;
appSettings.JobEngineConfig.CleanUpLogsIntervalHours = 48;
appSettings.JobEngineConfig.CleanUpLogsOlderThanHours = 48;

AppLogger appLogger = new AppLogger(logger);
JobEngine JE = new JobEngine(appLogger, appSettings.JobEngineConfig, "D:\\_Appoggio\\OSRobot\\Logs\\");
JE.Start();

Console.ReadLine();
*/


/*
XmlSerialization S = new XmlSerialization();
XmlDocument D = S.Serialize(rootFolder, "OSRobot");

StringWriter stringWriter = new StringWriter();
XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);

D.WriteTo(xmlTextWriter);

Console.WriteLine(stringWriter.ToString());

Console.WriteLine("End");
*/

/*
JsonSerializerOptions options = new()
{
    ReferenceHandler = ReferenceHandler.Preserve,
    WriteIndented = true
};

string output = JsonSerializer.Serialize<Folder>(rootFolder, options);

Console.WriteLine(output);

Console.WriteLine("End");

*/
/*

Console.WriteLine("OSRobot JobEngine is starting...");

Folder? rootFolder = JobsPersistence.LoadJobs("./");

Console.WriteLine("End");

*/
/*
var options = new JsonReaderOptions
{
    AllowTrailingCommas = true,
    CommentHandling = JsonCommentHandling.Skip
};
var reader = new Utf8JsonReader(UTF8Encoding.UTF8.GetBytes(JSONConfig), options);

while (reader.Read())
{
    Console.Write(reader.TokenType);

    switch (reader.TokenType)
    {
        case JsonTokenType.PropertyName:
        case JsonTokenType.String:
            {
                string? text = reader.GetString();
                Console.Write(" ");
                Console.Write(text);
                break;
            }

        case JsonTokenType.Number:
            {
                int intValue = reader.GetInt32();
                Console.Write(" ");
                Console.Write(intValue);
                break;
            }

            // Other token types elided for brevity
    }
    Console.WriteLine();
}

*/

/*
Dictionary<string, object>? Config = JsonSerializer.Deserialize<Dictionary<string, object>>(JSONConfig);

if (Config == null)
    return;
*/

/*
Dictionary<string, object>? Config = JsonConvert.DeserializeObject<Dictionary<string, object>>(JSONConfig);

if (Config == null)
    return;


Console.WriteLine("End");
*/
/*
Dictionary<string, object>? Config = JsonSerializer.Deserialize<Dictionary<string, object>>(JSONConfig);

if (Config == null)
    return;

var RootFolder = Config["rootFolder"];
if (RootFolder == null)
    return;

foreach (KeyValuePair<string, object> kvp in Config)
{
    Console.WriteLine($"{kvp.Key}" + ": " + $"{kvp.Value}");
}

Console.WriteLine("End");
*/
//Console.WriteLine(Config["RootFolder"]["Config"]);

/*
Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

ILogger logger = Log.Logger;

logger.Information("Hello, world!");

logger.Error(new Exception(""), "zzzz");
*/


//var Z = JobsPersistence.LoadJobEditorJSON("D:\\ProgettiDEV\\OSRobot\\OSRobot.Server\\_RuntimeFolders\\ClientDataPath/ClientData.json");

//./ _RuntimeFolders / ClientDataPath / ClientData.json

Console.WriteLine("Hello world!");