// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Data;
using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.RESTApiTask;

namespace OSRobot.Tests.IntegrationTests;

[TestClass]
[TestCategory(IntegrationGuard.Category)]
public sealed class IntegrationTestRESTApiTask
{
    [TestInitialize]
    public void RequireIntegrationEnvironment() => IntegrationGuard.RequireEnabled();

    [TestMethod]
    public async Task TestGet()
    {
        // ---------
        // Arrange
        // ---------
        Folder folder = Common.CreateRootFolder();

        RESTApiTaskConfig config = new()
        {
            Id = 1,
            Name = "REST Api task 1",

            // A public test API that returns JSON data
            URL = "https://jsonplaceholder.typicode.com/posts",
            Method = MethodType.Get,
            
            // Root of the JSON response
            JsonPathToData = "$", 
            ReturnsRecordset = true
        };

        RESTApiTask task = new()
        {
            ParentFolder = folder,
            Config = config
        };

        Common.ConfigureLogPath();
        (DynamicDataChain dynDataChain,
         DynamicDataSet dynDataSet,
         IPluginInstanceLogger logger) = Common.GetTaskDefaultParameters(task);

        // ---------
        // Act
        // ---------
        task.Init();
        InstanceExecResult result = await task.RunAsync(dynDataChain, dynDataSet, 0, logger, CancellationToken.None);
        task.Destroy();

        // ---------
        // Assert
        // ---------
        Assert.IsTrue(result.ExecResults.Count > 0, "There are no executions.");
        

        // Pinging 127.0.0.1 we expect a 100% success rate
        ExecResult execResult = result.ExecResults[0];
        Assert.IsTrue(execResult.Result, "Task failed.");
        DataTable data = (DataTable)execResult.Data["DefaultRecordset"];
        Assert.IsTrue(data.Rows.Count > 0, "No rows in recordset.");    
    }
}
