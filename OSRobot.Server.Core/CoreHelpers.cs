// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using OSRobot.Server.Core.Persistence;
using System.Text.Json;

namespace OSRobot.Server.Core;

public static class CoreHelpers
{
    public static string ToIsoDate(this DateTime date)
    {
        return date.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Deep-copies a config, or an item of one, through a JSON round-trip with the same options jobs.json
    /// is loaded with: configs only ever come from jobs.json, so a copy holds exactly what loading it does.
    /// Meant for configs: object graphs with cycles or interface-typed members (plugin instances, folders)
    /// cannot be cloned this way. Thread-safe: the shared options are immutable once in use.
    /// </summary>
    public static object? CloneObjects(object obj)
    {
        Type type = obj.GetType();
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(obj, type, JsonDeserialization.JobsJsonOptions);
        return JsonSerializer.Deserialize(json, type, JsonDeserialization.JobsJsonOptions);
    }

    /// <summary>
    /// Makes the instance that runs a task definition once. Only the config is copied: a deep copy of the
    /// whole task would also copy ParentFolder and Connections, i.e. the entire folder tree and every
    /// downstream object, making each run cost as much as the workspace is large.
    /// The clone only reads them (ParentFolder for its log path; successors are taken from the definition),
    /// so sharing the definition's graph is safe. Like the deep copy did, the new instance starts with
    /// its private state at defaults and is not Init()-ed.
    /// </summary>
    public static ITask CloneTaskForRun(ITask task)
    {
        ITask taskCopy = (ITask?)Activator.CreateInstance(task.GetType()) ?? throw new ApplicationException($"Cannot create an instance of {task.GetType().FullName}");
        taskCopy.Config = (IPluginInstanceConfig?)CloneObjects(task.Config) ?? throw new ApplicationException("Cloning configuration returned null");
        taskCopy.ParentFolder = task.ParentFolder;
        taskCopy.Connections = task.Connections;

        return taskCopy;
    }
}
