using OSRobot.Server.Core;
using OSRobot.Server.Core.DynamicData;

namespace OSRobot.Tests.TestClasses;

[TestClass]
public sealed class TestExecutionConditions
{
    private static ExecResult Result(bool succeeded, params (string Key, object Value)[] data)
    {
        DynamicDataSet set = [];
        foreach ((string key, object value) in data)
            set[key] = value;
        return new ExecResult(succeeded, set);
    }

    private static ExecutionCondition Condition(EnumExecutionConditionOperator op, string min = "", string max = "", string code = "") =>
        new(code, op, min, max);

    // ----- Success / failure operators ----------------------------------------------------------

    [TestMethod]
    public void ObjectExecutes_matches_only_a_successful_run()
    {
        ExecutionCondition c = Condition(EnumExecutionConditionOperator.ObjectExecutes);

        Assert.IsTrue(c.EvaluateCondition(Result(true)));
        Assert.IsFalse(c.EvaluateCondition(Result(false)));
    }

    [TestMethod]
    public void ObjectDoesNotExecute_matches_only_a_failed_run()
    {
        ExecutionCondition c = Condition(EnumExecutionConditionOperator.ObjectDoesNotExecute);

        Assert.IsTrue(c.EvaluateCondition(Result(false)));
        Assert.IsFalse(c.EvaluateCondition(Result(true)));
    }

    // ----- Value operators ----------------------------------------------------------------------

    [TestMethod]
    public void ValueEqualsTo_compares_the_text_of_the_value()
    {
        ExecutionCondition c = Condition(EnumExecutionConditionOperator.ValueEqualsTo, min: "42", code: "Count");

        Assert.IsTrue(c.EvaluateCondition(Result(true, ("Count", 42))));
        Assert.IsFalse(c.EvaluateCondition(Result(true, ("Count", 43))));
    }

    [TestMethod]
    public void Text_operators_match_contains_starts_with_and_ends_with()
    {
        ExecutionCondition contains = Condition(EnumExecutionConditionOperator.ValueContains, min: "err", code: "Msg");
        ExecutionCondition starts = Condition(EnumExecutionConditionOperator.ValueStartsWith, min: "Fatal", code: "Msg");
        ExecutionCondition ends = Condition(EnumExecutionConditionOperator.ValueEndsWith, min: "done", code: "Msg");

        ExecResult r = Result(true, ("Msg", "Fatal error: all done"));

        Assert.IsTrue(contains.EvaluateCondition(r));
        Assert.IsTrue(starts.EvaluateCondition(r));
        Assert.IsTrue(ends.EvaluateCondition(r));
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueContains, min: "warning", code: "Msg").EvaluateCondition(r));
    }

    [TestMethod]
    public void Numeric_operators_work_on_integer_values()
    {
        ExecResult r = Result(true, ("Count", 10));

        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "5", code: "Count").EvaluateCondition(r));
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "10", code: "Count").EvaluateCondition(r));
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueLessThan, min: "11", code: "Count").EvaluateCondition(r));
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueLessThan, min: "10", code: "Count").EvaluateCondition(r));
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueBetween, min: "10", max: "20", code: "Count").EvaluateCondition(r), "Between is inclusive.");
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueBetween, min: "11", max: "20", code: "Count").EvaluateCondition(r));
    }

    [TestMethod]
    [Ignore("Known production bug: ExecutionCondition unboxes values with (int), so ValueGreaterThan/LessThan/Between throw " +
            "InvalidCastException for any non-int value (float, long, double, decimal, numeric strings). Remove this attribute once fixed.")]
    public void Numeric_operators_work_on_a_float_value_such_as_PingTasks_ThresholdSuccessRate()
    {
        // PingTask publishes ThresholdSuccessRate as a float (see its sample dynamic data: "50%").
        ExecResult r = Result(true, ("ThresholdSuccessRate", 75f));

        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "50", code: "ThresholdSuccessRate").EvaluateCondition(r));
    }

    [TestMethod]
    [Ignore("Known production bug: ExecutionCondition unboxes values with (int), so ValueGreaterThan/LessThan/Between throw " +
            "InvalidCastException for any non-int value (float, long, double, decimal, numeric strings). Remove this attribute once fixed.")]
    public void Numeric_operators_work_on_a_long_value_such_as_a_tick_count()
    {
        ExecResult r = Result(true, ("ExecutionStartDateTicks", 638_000_000_000_000_000L));

        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "1", code: "ExecutionStartDateTicks").EvaluateCondition(r));
    }

    // ----- Combining conditions on a connection -------------------------------------------------

    private static PluginInstanceConnection Connection(List<ExecutionCondition> execute, List<ExecutionCondition>? dontExecute = null) =>
        new() { Enabled = true, ExecuteConditions = execute, DontExecuteConditions = dontExecute ?? [] };

    [TestMethod]
    public void A_connection_with_no_execute_conditions_never_fires()
    {
        Assert.IsFalse(Connection([]).EvaluateExecConditions(Result(true)));
    }

    [TestMethod]
    public void A_connection_fires_when_any_execute_condition_matches()
    {
        PluginInstanceConnection c = Connection([
            Condition(EnumExecutionConditionOperator.ValueEqualsTo, min: "nope", code: "Msg"),
            Condition(EnumExecutionConditionOperator.ObjectExecutes)
        ]);

        Assert.IsTrue(c.EvaluateExecConditions(Result(true, ("Msg", "hello"))));
    }

    [TestMethod]
    public void A_matching_dont_execute_condition_blocks_the_connection()
    {
        PluginInstanceConnection c = Connection(
            execute: [Condition(EnumExecutionConditionOperator.ObjectExecutes)],
            dontExecute: [Condition(EnumExecutionConditionOperator.ValueContains, min: "skip", code: "Msg")]);

        Assert.IsTrue(c.EvaluateExecConditions(Result(true, ("Msg", "carry on"))));
        Assert.IsFalse(c.EvaluateExecConditions(Result(true, ("Msg", "please skip me"))));
    }
}
