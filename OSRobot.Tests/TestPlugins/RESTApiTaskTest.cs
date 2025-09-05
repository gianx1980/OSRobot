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

using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging.Abstract;
using OSRobot.Server.Plugins.PingTask;
using OSRobot.Server.Plugins.RESTApiTask;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSRobot.Tests.TestPlugins;

[TestClass]
public sealed class RESTApiTaskTest
{
    [TestMethod]
    public void TestGet()
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
        InstanceExecResult result = task.Run(dynDataChain, dynDataSet, 0, logger);
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
