using OSRobot.Server.Core;
using OSRobot.Server.Core.Persistence;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestCloneObjectsConcurrency
{
    private const int ClonesPerPlugin = 200;

    private static string Serialize(object o) =>
        new XmlSerialization { CheckSerializeAttribute = true }.SerializeToXmlString(o, "OSRobot");

    [TestMethod]
    public void CloneObjects_is_correct_when_called_concurrently_for_every_plugin()
    {
        // One default-configured instance per plugin, and the expected serialized form of each.
        List<(string Id, IPluginInstance Instance, string Expected)> subjects = [];
        foreach (IPlugin plugin in PluginRegistry.GetPlugins())
        {
            IPluginInstance instance = plugin.GetInstance();
            instance.Config = plugin.GetPluginDefaultConfig();
            subjects.Add((plugin.Id, instance, Serialize(instance)));
        }

        // Clone every plugin ClonesPerPlugin times, all interleaved across many threads.
        List<string> failures = [];
        object failuresGate = new();

        Parallel.For(0, subjects.Count * ClonesPerPlugin, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 4 }, i =>
        {
            (string id, IPluginInstance instance, string expected) = subjects[i % subjects.Count];
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
