using OSRobot.Server.Core;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestPluginRegistry
{
    [TestMethod]
    public void Discovers_all_built_in_plugins_with_unique_ids()
    {
        // The test assembly declares plugins of its own (see Support/), so count only the built-in assembly.
        List<IPlugin> plugins = [.. PluginRegistry.GetPlugins().Where(p => p.GetType().Assembly == typeof(OSRobot.Server.Plugins.PingTask.PingTaskPlugin).Assembly)];

        // One *Plugin.cs per plugin folder in OSRobot.Server.Plugins.
        Assert.HasCount(23, plugins);
        Assert.HasCount(plugins.Count, plugins.Select(p => p.Id).Distinct());
    }

    [TestMethod]
    public void GetPlugin_resolves_by_id_and_rejects_unknown_ids()
    {
        foreach (IPlugin plugin in PluginRegistry.GetPlugins())
        {
            IPlugin? resolved = PluginRegistry.GetPlugin(plugin.Id);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(plugin.GetType(), resolved.GetType());
            Assert.IsNotNull(plugin.GetInstance());
            Assert.IsNotNull(plugin.GetPluginDefaultConfig());
        }

        Assert.IsNull(PluginRegistry.GetPlugin("NoSuchPlugin"));
    }

    [TestMethod]
    public void Discovers_plugins_declared_outside_the_built_in_assembly()
    {
        // TestProbeTaskPlugin / TestTriggerEventPlugin live in this test assembly: found the same way a
        // third-party plugin assembly would be, with no registration step.
        IPlugin? probe = PluginRegistry.GetPlugin("TestProbeTask");
        IPlugin? trigger = PluginRegistry.GetPlugin("TestTriggerEvent");

        Assert.IsNotNull(probe);
        Assert.IsNotNull(trigger);
        Assert.AreEqual(EnumPluginType.Task, probe.PluginType);
        Assert.AreEqual(EnumPluginType.Event, trigger.PluginType);
        Assert.AreEqual(typeof(OSRobot.Tests.Support.TestProbeTask), probe.GetInstance().GetType());
    }

    [TestMethod]
    public void Every_plugin_declares_a_matching_instance_and_config_type()
    {
        foreach (IPlugin plugin in PluginRegistry.GetPlugins())
        {
            IPluginInstance instance = plugin.GetInstance();
            IPluginInstanceConfig config = plugin.GetPluginDefaultConfig();

            Assert.AreEqual(plugin.PluginType == EnumPluginType.Event, instance is IEvent, $"{plugin.Id}: PluginType disagrees with the instance type.");
            Assert.AreEqual(plugin.PluginType == EnumPluginType.Task, instance is ITask, $"{plugin.Id}: PluginType disagrees with the instance type.");
            Assert.AreEqual(plugin.PluginType == EnumPluginType.Task, config is ITaskConfig, $"{plugin.Id}: PluginType disagrees with the config type.");
        }
    }
}
