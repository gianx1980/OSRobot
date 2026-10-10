// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections;
using System.Reflection;

namespace OSRobot.Server.Core.DynamicData;

public class DynamicDataIssue(int objectId, string objectName, string location, string reference, string message)
{
    public int ObjectId { get; private set; } = objectId;
    public string ObjectName { get; private set; } = objectName;
    /// <summary>Where the reference is, e.g. "ColumnsDefinition[2].FieldValue" or "Connection to 5".</summary>
    public string Location { get; private set; } = location;
    public string Reference { get; private set; } = reference;
    public string Message { get; private set; } = message;

    public override string ToString()
    {
        return $"{ObjectId}:{ObjectName} - {Location} - {Reference}: {Message}";
    }
}

/// <summary>
/// Checks the dynamic data references of a job workspace without running it, so a broken
/// reference shows up when the job is saved instead of failing the task at runtime.
/// A reference {object[N].Field...} used by object X is valid when:
/// - object N exists;
/// - N runs before X, i.e. X can be reached from N through connections (at runtime only
///   the objects along the executed path are in the dynamic data chain);
/// - N outputs Field (as declared by its plugin's SampleDynamicData; names are case-sensitive).
/// A task iterating over an object recordset must name it with such a reference, to a recordset field.
/// [CODE] expressions are not checked, as that would require running them.
/// </summary>
public static class DynamicDataValidator
{
    private const string _codePlaceholder = "[CODE]";

    public static List<DynamicDataIssue> Validate(IFolder rootFolder)
    {
        List<IPluginInstance> instances = [];
        CollectInstances(rootFolder, instances);

        Dictionary<int, IPluginInstance> instancesById = [];
        foreach (IPluginInstance instance in instances)
            instancesById.TryAdd(instance.Config.Id, instance);

        Dictionary<int, List<int>> predecessors = BuildPredecessors(instances);
        Dictionary<Type, HashSet<string>> outputsByType = BuildOutputsByType(recordsetsOnly: false);
        Dictionary<Type, HashSet<string>> recordsetsByType = BuildOutputsByType(recordsetsOnly: true);

        List<DynamicDataIssue> issues = [];

        foreach (IPluginInstance instance in instances)
        {
            HashSet<int> upstream = GetUpstream(instance.Config.Id, predecessors);

            foreach ((string location, string value) in GetConfigStrings(instance.Config))
            {
                List<DynamicDataInfo> references = value.StartsWith(_codePlaceholder) ? [] : DynamicDataParser.GetDynamicDataInfo(value);

                // The iteration object is resolved as a plain reference: [CODE] is not evaluated there, and text
                // without braces is not a reference at all.
                bool isIterationObject = location == nameof(ITaskConfig.IterationObject);
                if (isIterationObject && references.Count == 0)
                    issues.Add(new DynamicDataIssue(instance.Config.Id, instance.Config.Name, location, value,
                                                    "The iteration object must be a dynamic data reference such as {object[N].FieldName}, e.g. {object[2].DefaultRecordset}."));

                foreach (DynamicDataInfo info in references)
                {
                    string? message = CheckReference(info, instance.Config.Id, instancesById, upstream, outputsByType);
                    if (message == null && isIterationObject && !IsRecordset(instancesById[info.ObjectID], info.FieldName, recordsetsByType))
                        message = $"Field '{info.FieldName}' of object {info.ObjectID} ({instancesById[info.ObjectID].Config.Name}) is not a recordset, so it can't be iterated.";

                    if (message != null)
                        issues.Add(new DynamicDataIssue(instance.Config.Id, instance.Config.Name, location, info.DynamicData, message));
                }
            }

            foreach (PluginInstanceConnection connection in instance.Connections)
            {
                IEnumerable<ExecutionCondition> conditions = connection.ExecuteConditions.Concat(connection.DontExecuteConditions);
                foreach (ExecutionCondition condition in conditions)
                {
                    // ObjectExecutes / ObjectDoesNotExecute only look at the result, not at a value.
                    if (condition.Operator == EnumExecutionConditionOperator.ObjectExecutes
                        || condition.Operator == EnumExecutionConditionOperator.ObjectDoesNotExecute)
                        continue;

                    string? message = condition.DynamicDataCode == CommonDynamicData.Results
                        ? $"The {CommonDynamicData.Results} recordset holds all the iterations' data: it can't be compared to a value in a condition."
                        : CheckField(instance, condition.DynamicDataCode ?? string.Empty, outputsByType);
                    if (message != null)
                        issues.Add(new DynamicDataIssue(instance.Config.Id, instance.Config.Name, $"Connection to {connection.ConnectTo?.Config.Id}",
                                                        condition.DynamicDataCode ?? string.Empty, message));
                }
            }
        }

        return issues;
    }

    private static string? CheckReference(DynamicDataInfo info, int referencingId, Dictionary<int, IPluginInstance> instancesById, HashSet<int> upstream,
                                          Dictionary<Type, HashSet<string>> outputsByType)
    {
        if (!instancesById.TryGetValue(info.ObjectID, out IPluginInstance? referenced))
            return $"Object {info.ObjectID} does not exist.";

        if (!upstream.Contains(info.ObjectID))
            return $"Object {info.ObjectID} ({referenced.Config.Name}) does not run before this object, so its data is not available here. Connect it upstream of this object.";

        string? message = CheckField(referenced, info.FieldName, outputsByType);
        if (message != null)
            return message;

        if (info.FieldName == CommonDynamicData.Results && !CollectsResultsTowards(referenced, referencingId, upstream))
            return $"Object {info.ObjectID} ({referenced.Config.Name}) passes on '{CommonDynamicData.Results}' only through a connection that runs once with all results, " +
                   "and none of its connections leading here is set that way.";

        return null;
    }

    // Whether the referenced object has a "once with all results" connection on a path to the referencing object:
    // directly to it, or to an object upstream of it.
    private static bool CollectsResultsTowards(IPluginInstance referenced, int referencingId, HashSet<int> upstream)
    {
        return referenced.Connections.Any(c => c.RunMode == EnumConnectionRunMode.OnceWithAllResults
                                               && c.ConnectTo != null
                                               && (c.ConnectTo.Config.Id == referencingId || upstream.Contains(c.ConnectTo.Config.Id)));
    }

    private static string? CheckField(IPluginInstance instance, string fieldName, Dictionary<Type, HashSet<string>> outputsByType)
    {
        // An unknown plugin type declares nothing to check against.
        if (!outputsByType.TryGetValue(instance.GetType(), out HashSet<string>? outputs))
            return null;

        if (outputs.Contains(fieldName))
            return null;

        string? differentCase = outputs.FirstOrDefault(o => string.Equals(o, fieldName, StringComparison.OrdinalIgnoreCase));
        if (differentCase != null)
            return $"Object {instance.Config.Id} ({instance.Config.Name}) has no field '{fieldName}'. Field names are case-sensitive: did you mean '{differentCase}'?";

        return $"Object {instance.Config.Id} ({instance.Config.Name}) has no field '{fieldName}'.";
    }

    private static void CollectInstances(IFolder folder, List<IPluginInstance> instances)
    {
        foreach (IPluginInstanceBase item in folder.Items)
        {
            if (item is IFolder innerFolder)
                CollectInstances(innerFolder, instances);
            else if (item is IPluginInstance instance)
                instances.Add(instance);
        }
    }

    private static Dictionary<int, List<int>> BuildPredecessors(List<IPluginInstance> instances)
    {
        Dictionary<int, List<int>> predecessors = [];

        foreach (IPluginInstance source in instances)
        {
            foreach (PluginInstanceConnection connection in source.Connections)
            {
                if (connection.ConnectTo == null)
                    continue;

                int targetId = connection.ConnectTo.Config.Id;
                if (!predecessors.TryGetValue(targetId, out List<int>? list))
                    predecessors[targetId] = list = [];
                list.Add(source.Config.Id);
            }
        }

        return predecessors;
    }

    // All the objects from which objectId can be reached through connections.
    private static HashSet<int> GetUpstream(int objectId, Dictionary<int, List<int>> predecessors)
    {
        HashSet<int> upstream = [];
        Queue<int> toVisit = new();
        toVisit.Enqueue(objectId);

        while (toVisit.Count > 0)
        {
            int current = toVisit.Dequeue();
            if (!predecessors.TryGetValue(current, out List<int>? list))
                continue;

            foreach (int predecessor in list)
            {
                if (upstream.Add(predecessor))
                    toVisit.Enqueue(predecessor);
            }
        }

        // A task's own data is not in the chain while it runs, even when a loop leads back to it.
        upstream.Remove(objectId);

        return upstream;
    }

    private static bool IsRecordset(IPluginInstance instance, string fieldName, Dictionary<Type, HashSet<string>> recordsetsByType)
    {
        // An unknown plugin type declares nothing to check against.
        return !recordsetsByType.TryGetValue(instance.GetType(), out HashSet<string>? recordsets) || recordsets.Contains(fieldName);
    }

    private static Dictionary<Type, HashSet<string>> BuildOutputsByType(bool recordsetsOnly)
    {
        Dictionary<Type, HashSet<string>> outputsByType = [];

        foreach (IPlugin plugin in PluginRegistry.GetPlugins())
        {
            HashSet<string> outputs = new(CommonDynamicData.GetOutputSamples(plugin).Where(s => !recordsetsOnly || s.IsRecordset).Select(s => s.InternalName),
                                          StringComparer.Ordinal);
            outputsByType.TryAdd(plugin.GetInstance().GetType(), outputs);
        }

        return outputsByType;
    }

    // Every string in the configuration, including the ones nested in lists and sub-objects
    // (column definitions, copy items, headers, ...): plugins resolve dynamic data in those too.
    private static IEnumerable<(string Location, string Value)> GetConfigStrings(IPluginInstanceConfig config)
    {
        List<(string, string)> result = [];
        CollectStrings(config, string.Empty, result, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);

        // The iteration object is only used when iterating over an object recordset: a stale
        // value left there from a previous setting is never resolved.
        if (config is ITaskConfig taskConfig && taskConfig.PluginIterationMode != IterationMode.IterateObjectRecordset)
            result.RemoveAll(r => r.Item1 == nameof(ITaskConfig.IterationObject));

        return result;
    }

    private static void CollectStrings(object? value, string path, List<(string, string)> result, HashSet<object> visited, int depth)
    {
        if (value == null || depth > 10)
            return;

        if (value is string s)
        {
            result.Add((path, s));
            return;
        }

        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is DateTime || value is decimal || !visited.Add(value))
            return;

        if (value is IEnumerable enumerable)
        {
            int index = 0;
            foreach (object? item in enumerable)
                CollectStrings(item, $"{path}[{index++}]", result, visited, depth + 1);
            return;
        }

        // Only walk OSRobot's own configuration classes, not framework types.
        if (type.Namespace == null || !type.Namespace.StartsWith("OSRobot"))
            return;

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            string propPath = path.Length == 0 ? prop.Name : $"{path}.{prop.Name}";
            CollectStrings(prop.GetValue(value), propPath, result, visited, depth + 1);
        }
    }
}
