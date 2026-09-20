using OSRobot.Server.Core;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestPluginRegistry
{
    [TestMethod]
    public void Discovers_all_built_in_plugins_with_unique_ids()
    {
        List<IPlugin> plugins = PluginRegistry.GetPlugins();

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
}
