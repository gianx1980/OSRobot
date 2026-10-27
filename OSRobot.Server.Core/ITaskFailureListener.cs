// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core;

/// <param name="Task">The task definition that failed (not the clone that ran): its Config and ParentFolder.</param>
/// <param name="IterationIndex">The failed iteration, or null when the task failed as a whole (e.g. an error outside its iterations).</param>
/// <param name="ErrorMessage">Why it failed, secrets redacted.</param>
/// <param name="Handled">True if one of the task's connections started something for this failure
/// (e.g. an "Object does not execute" connection): the job already reacts to it.</param>
public sealed record TaskFailure(ITask Task, int? IterationIndex, string ErrorMessage, DateTime FailureDate, bool Handled);

/// <summary>
/// An event that wants to know about task failures (the Task failure event). The engine calls
/// <see cref="OnTaskFailed"/> from its worker threads, once per failed iteration, after the task's
/// connections were dispatched: implementations must be thread-safe, return quickly, and publish from
/// a thread of their own. Failures of runs started by such an event are not reported, so an event
/// whose own job fails (a mail server that is down) can't trigger itself in a loop.
/// </summary>
public interface ITaskFailureListener
{
    void OnTaskFailed(TaskFailure failure);
}
