using OSRobot.Server.Core.DynamicData;
using OSRobot.Server.Core.Logging;

namespace OSRobot.Tests.TestClasses;

/// <summary>
/// [CODE] C# expressions. Kept apart from the plain parser tests because the on/off switch
/// (Core.ScriptingEnabled) is process-wide state, so these must not run in parallel with anything else.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TestDynamicDataScripting
{
    private static DynamicDataChain Chain() => new()
    {
        [1] = new DynamicDataSet { ["Name"] = "world" }
    };

    private static void SetScripting(bool enabled) =>
        OSRobot.Server.Core.Core.Init(PluginInstanceLogger.LogPath, enabled);

    [TestCleanup]
    public void RestoreDefault() => SetScripting(true);

    [TestMethod]
    public void A_code_expression_is_evaluated_against_the_dynamic_data_chain()
    {
        SetScripting(true);

        string result = DynamicDataParser.ReplaceDynamicData("[CODE]\"hello \" + dynamicDataChain[1][\"Name\"] + \" #\" + iterationNumber", Chain(), 3, null);

        Assert.AreEqual("hello world #3", result);
    }

    [TestMethod]
    public void Code_expressions_are_refused_when_scripting_is_disabled()
    {
        SetScripting(false);

        ApplicationException ex = Assert.Throws<ApplicationException>(
            () => DynamicDataParser.ReplaceDynamicData("[CODE]\"anything\"", Chain(), 0, null));

        StringAssert.Contains(ex.Message, "disabled");
    }

    [TestMethod]
    public void Plain_placeholders_still_work_when_scripting_is_disabled()
    {
        SetScripting(false);

        Assert.AreEqual("world", DynamicDataParser.ReplaceDynamicData("{object[1].Name}", Chain(), 0, null));
    }
}
