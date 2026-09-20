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
using System.Reflection;

namespace OSRobot.Server.Core;

/// <summary>
/// Discovers plugins by scanning assemblies for concrete <see cref="IPlugin"/> implementations
/// with a public parameterless constructor. Adding a plugin therefore needs no registration
/// step: implement IPlugin in an assembly the registry scans and it is picked up.
///
/// Scanned assemblies: everything already loaded in the AppDomain, plus any OSRobot*.dll in the
/// application directory that isn't loaded yet (project-referenced assemblies are loaded lazily,
/// so they might not be in the AppDomain the first time the registry is touched).
/// </summary>
public static class PluginRegistry
{
    private const string AssemblyFilePattern = "OSRobot*.dll";

    private static readonly Lazy<IReadOnlyDictionary<string, Type>> _pluginTypes = new(Discover, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Returns a fresh instance of every discovered plugin.</summary>
    public static List<IPlugin> GetPlugins()
    {
        return [.. _pluginTypes.Value.Values.Select(CreatePlugin).OrderBy(p => p.Id, StringComparer.Ordinal)];
    }

    /// <summary>Returns a fresh instance of the plugin with the given id, or null if unknown.</summary>
    public static IPlugin? GetPlugin(string pluginId)
    {
        return _pluginTypes.Value.TryGetValue(pluginId, out Type? pluginType) ? CreatePlugin(pluginType) : null;
    }

    private static IPlugin CreatePlugin(Type pluginType) => (IPlugin)Activator.CreateInstance(pluginType)!;

    private static Dictionary<string, Type> Discover()
    {
        Dictionary<string, Type> result = new(StringComparer.Ordinal);

        foreach (Assembly assembly in GetCandidateAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!typeof(IPlugin).IsAssignableFrom(type)
                    || type.IsAbstract
                    || type.IsInterface
                    || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                string id = CreatePlugin(type).Id;
                if (!result.TryAdd(id, type))
                    throw new InvalidOperationException($"Duplicate plugin id '{id}': {result[id].FullName} and {type.FullName}.");
            }
        }

        return result;
    }

    private static List<Assembly> GetCandidateAssemblies()
    {
        List<Assembly> assemblies = [.. AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)];
        HashSet<string> loadedNames = [.. assemblies.Select(a => a.GetName().Name!)];

        foreach (string file in Directory.EnumerateFiles(AppContext.BaseDirectory, AssemblyFilePattern))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (loadedNames.Contains(name))
                continue;

            try
            {
                assemblies.Add(Assembly.Load(new AssemblyName(name)));
                loadedNames.Add(name);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
            {
                // Not a managed assembly (or not loadable): can't contain plugins.
            }
        }

        return assemblies;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}
