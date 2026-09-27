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
    public void Numeric_operators_work_on_a_float_value_such_as_PingTasks_ThresholdSuccessRate()
    {
        // PingTask publishes ThresholdSuccessRate as a float (see its sample dynamic data: "50%").
        ExecResult r = Result(true, ("ThresholdSuccessRate", 75f));

        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "50", code: "ThresholdSuccessRate").EvaluateCondition(r));
    }

    [TestMethod]
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

    [TestMethod]
    [DataRow(75.5, "75", "80", true)]
    [DataRow(75.5, "75.5", "75.5", true)]
    [DataRow(75.5, "76", "80", false)]
    public void ValueBetween_supports_decimal_bounds_and_values(double value, string min, string max, bool expected)
    {
        ExecutionCondition c = Condition(EnumExecutionConditionOperator.ValueBetween, min: min, max: max, code: "V");

        Assert.AreEqual(expected, c.EvaluateCondition(Result(true, ("V", value))));
    }

    [TestMethod]
    public void Numeric_operators_accept_numeric_text_and_other_numeric_types()
    {
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", "10"))));
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", 10m))));
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueLessThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", (short)3))));
        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", 6.5d))));
    }

    [TestMethod]
    public void Long_values_are_compared_exactly()
    {
        // Beyond what a double can represent exactly: 2^60 + 1 versus 2^60.
        ExecResult r = Result(true, ("V", 1_152_921_504_606_846_977L));

        Assert.IsTrue(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "1152921504606846976", code: "V").EvaluateCondition(r));
    }

    [TestMethod]
    public void A_non_numeric_value_does_not_satisfy_a_numeric_condition_instead_of_throwing()
    {
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", "not a number"))));
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueLessThan, min: "5", code: "V").EvaluateCondition(Result(true, ("V", double.NaN))));
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueBetween, min: "1", max: "9", code: "V").EvaluateCondition(Result(true, ("V", new object()))));
    }

    [TestMethod]
    public void An_unparseable_threshold_does_not_satisfy_the_condition_instead_of_throwing()
    {
        Assert.IsFalse(Condition(EnumExecutionConditionOperator.ValueGreaterThan, min: "abc", code: "V").EvaluateCondition(Result(true, ("V", 10))));
    }
}
