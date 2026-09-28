using TaskBoard.Application.Services.Tasks;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Tests.Services.Tasks;

[TestFixture]
public sealed class TaskWorkflowTests
{
    private static readonly (TaskItemStatus From, TaskItemStatus To)[] AllowedTransitions =
    [
        (TaskItemStatus.Created, TaskItemStatus.InProgress),
        (TaskItemStatus.InProgress, TaskItemStatus.Completed),
        (TaskItemStatus.Completed, TaskItemStatus.Closed),
    ];

    private static IEnumerable<TestCaseData> AllTransitions() =>
        from current in Enum.GetValues<TaskItemStatus>()
        from target in Enum.GetValues<TaskItemStatus>()
        let allowed = AllowedTransitions.Contains((current, target))
        select new TestCaseData(current, target, allowed)
            .SetName($"IsTransitionAllowed_{current}To{target}_Returns{allowed}");

    [TestCaseSource(nameof(AllTransitions))]
    public void IsTransitionAllowed_OnlyImmediateNextStatusIsAllowed(
        TaskItemStatus from,
        TaskItemStatus to,
        bool expected)
    {
        Assert.That(TaskWorkflow.IsTransitionAllowed(from, to), Is.EqualTo(expected));
    }

    [TestCase(TaskItemStatus.Created, TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Completed)]
    [TestCase(TaskItemStatus.Completed, TaskItemStatus.Closed)]
    public void GetNextStatus_ForNonTerminalStatus_ReturnsFollowingStatus(
        TaskItemStatus current,
        TaskItemStatus expected)
    {
        Assert.That(TaskWorkflow.GetNextStatus(current), Is.EqualTo(expected));
    }

    [Test]
    public void GetNextStatus_ForClosed_ReturnsNull()
    {
        Assert.That(TaskWorkflow.GetNextStatus(TaskItemStatus.Closed), Is.Null);
    }

    [TestCase(TaskItemStatus.Created, true)]
    [TestCase(TaskItemStatus.InProgress, true)]
    [TestCase(TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Closed, false)]
    public void IsCoreEditable_OnlyBeforeCompletion(TaskItemStatus status, bool expected)
    {
        Assert.That(TaskWorkflow.IsCoreEditable(status), Is.EqualTo(expected));
    }

    [TestCase(TaskItemStatus.Created, true)]
    [TestCase(TaskItemStatus.InProgress, true)]
    [TestCase(TaskItemStatus.Completed, false)]
    [TestCase(TaskItemStatus.Closed, false)]
    public void IsDeletable_OnlyBeforeCompletion(TaskItemStatus status, bool expected)
    {
        Assert.That(TaskWorkflow.IsDeletable(status), Is.EqualTo(expected));
    }
}
