// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using OSRobot.Server.Core;

namespace OSRobot.Tests.TestClasses;

// Not parallelizable: this stress test saturates the thread pool on purpose, which delays the
// timer callbacks the event plugin tests depend on by several seconds when they run alongside it.
[TestClass]
[DoNotParallelize]
public sealed class TestCloneObjectsConcurrency
{
    private const int ClonesPerPlugin = 200;

    private static string Serialize(object o) => JsonSerializer.Serialize(o, o.GetType());

    [TestMethod]
    public void CloneObjects_is_correct_when_called_concurrently_for_every_plugin()
    {
        // One default config per plugin, and the expected serialized form of each.
        List<(string Id, IPluginInstanceConfig Instance, string Expected)> subjects = [];
        foreach (IPlugin plugin in PluginRegistry.GetPlugins())
        {
            IPluginInstanceConfig instance = plugin.GetPluginDefaultConfig();
            subjects.Add((plugin.Id, instance, Serialize(instance)));
        }

        // Clone every plugin ClonesPerPlugin times, all interleaved across many threads.
        List<string> failures = [];
        object failuresGate = new();

        Parallel.For(0, subjects.Count * ClonesPerPlugin, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            (string id, IPluginInstanceConfig instance, string expected) = subjects[i % subjects.Count];
            try
            {
                object? clone = CoreHelpers.CloneObjects(instance);

                if (clone == null || ReferenceEquals(clone, instance) || clone.GetType() != instance.GetType())
                    throw new InvalidOperationException("clone is null, the same reference, or the wrong type");

                string actual = Serialize(clone);
                if (actual != expected)
                    throw new InvalidOperationException("clone differs from the original");
            }
            catch (Exception ex)
            {
                lock (failuresGate)
                    failures.Add($"{id}: {ex.GetType().Name}: {ex.Message}");
            }
        });

        Assert.IsEmpty(failures, $"{failures.Count} clone(s) failed:\n{string.Join("\n", failures.Distinct().Take(20))}");
    }
}
