// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections;
using System.Reflection;
using OSRobot.Server.Core;

namespace OSRobot.Tests.TestClasses;

/// <summary>
/// Every plugin's config, filled with non-default values, must come out of CoreHelpers.CloneObjects
/// identical and fully independent. The comparison walks the objects by reflection, so it does not
/// depend on the serializer the clone uses.
/// </summary>
[TestClass]
public sealed class TestConfigCloning
{
    public static IEnumerable<object[]> PluginIds => PluginRegistry.GetPlugins().Select(p => new object[] { p.Id });

    [TestMethod]
    [DynamicData(nameof(PluginIds))]
    public void Filled_config_is_cloned_identically_and_independently(string pluginId)
    {
        IPluginInstanceConfig original = PluginRegistry.GetPlugin(pluginId)!.GetPluginDefaultConfig();
        Fill(original, depth: 0);

        object clone = CoreHelpers.CloneObjects(original)!;

        List<string> differences = [];
        Compare(original, clone, original.GetType().Name, differences);
        Assert.IsEmpty(differences, string.Join(Environment.NewLine, differences));
    }

    private static IEnumerable<PropertyInfo> DataProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead);

    private static bool IsScalar(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);

    // Distinct per property, so a value landing in the wrong property is caught too.
    private static object? SampleValue(Type type, string name, int depth)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            return SampleValue(underlying, name, depth);

        int seed = Math.Abs(name.GetHashCode() % 1000) + 2;
        if (type == typeof(string)) return $"{name}-value";
        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return seed;
        if (type == typeof(long)) return (long)seed;
        if (type == typeof(short)) return (short)seed;
        if (type == typeof(byte)) return (byte)(seed % 250 + 1);
        if (type == typeof(double)) return seed + 0.25;
        if (type == typeof(float)) return seed + 0.5f;
        if (type == typeof(decimal)) return seed + 0.75m;
        if (type == typeof(DateTime)) return new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Local);
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }
        if (type.IsArray)
        {
            Array array = Array.CreateInstance(type.GetElementType()!, 1);
            array.SetValue(SampleValue(type.GetElementType()!, name, depth + 1), 0);
            return array;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            IList list = (IList)Activator.CreateInstance(type)!;
            list.Add(SampleValue(type.GetGenericArguments()[0], name, depth + 1));
            return list;
        }
        if (depth < 4 && type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)
        {
            object instance = Activator.CreateInstance(type)!;
            Fill(instance, depth + 1);
            return instance;
        }
        return null;
    }

    private static void Fill(object target, int depth)
    {
        foreach (PropertyInfo property in DataProperties(target.GetType()).Where(p => p.SetMethod?.IsPublic == true))
        {
            object? value = SampleValue(property.PropertyType, property.Name, depth);
            if (value != null)
                property.SetValue(target, value);
        }
    }

    private static void Compare(object? expected, object? actual, string path, List<string> differences)
    {
        if (expected == null || actual == null)
        {
            if (expected != actual)
                differences.Add($"{path}: expected {expected ?? "null"}, got {actual ?? "null"}");
            return;
        }

        Type type = expected.GetType();
        if (IsScalar(type))
        {
            if (!expected.Equals(actual))
                differences.Add($"{path}: expected {expected}, got {actual}");
            return;
        }

        if (ReferenceEquals(expected, actual))
        {
            differences.Add($"{path}: the clone shares this object with the original");
            return;
        }

        if (expected is IList expectedList)
        {
            IList actualList = (IList)actual;
            if (expectedList.Count != actualList.Count)
            {
                differences.Add($"{path}: expected {expectedList.Count} items, got {actualList.Count}");
                return;
            }
            for (int i = 0; i < expectedList.Count; i++)
                Compare(expectedList[i], actualList[i], $"{path}[{i}]", differences);
            return;
        }

        if (actual.GetType() != type)
        {
            differences.Add($"{path}: expected type {type.Name}, got {actual.GetType().Name}");
            return;
        }

        foreach (PropertyInfo property in DataProperties(type))
            Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", differences);
    }
}
