using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class DiceSessionStateTests
{
    static RollOutcome Outcome(ulong id, string contentId, bool valid = true) =>
        new(
            id,
            HullKind.D6,
            valid,
            valid ? new FaceSlotId(6) : FaceSlotId.None,
            valid ? new FaceContent { Id = contentId, Label = contentId } : FaceContent.None);

    [Fact]
    public void Record_stores_outcome_by_instance_id()
    {
        var session = new DiceSessionState();
        session.Record(Outcome(11, "6"));

        Assert.True(session.TryGet(11, out var stored));
        Assert.Equal("6", stored.Content.Id);
        Assert.Equal(1, session.Count);
    }

    [Fact]
    public void Record_overwrites_same_instance_id()
    {
        var session = new DiceSessionState();
        session.Record(Outcome(11, "6"));
        session.Record(Outcome(11, "sword"));

        Assert.Equal(1, session.Count);
        Assert.True(session.TryGet(11, out var stored));
        Assert.Equal("sword", stored.Content.Id);
    }

    [Fact]
    public void Forget_removes_instance_so_it_is_not_a_ghost()
    {
        var session = new DiceSessionState();
        session.Record(Outcome(11, "6"));
        session.Record(Outcome(22, "1"));

        session.Forget(11);

        Assert.Equal(1, session.Count);
        Assert.False(session.TryGet(11, out _));
        Assert.True(session.TryGet(22, out _));
    }

    [Fact]
    public void CountByContentId_counts_valid_matches_only()
    {
        var session = new DiceSessionState();
        session.Record(Outcome(11, "sword"));
        session.Record(Outcome(22, "6"));
        session.Record(Outcome(33, "sword", valid: false));

        Assert.Equal(1, session.CountByContentId("sword"));
    }

    [Fact]
    public void CountByContentId_is_zero_after_forget()
    {
        var session = new DiceSessionState();
        session.Record(Outcome(11, "sword"));
        session.Forget(11);

        Assert.Equal(0, session.CountByContentId("sword"));
        Assert.Empty(session.Outcomes);
    }
}
