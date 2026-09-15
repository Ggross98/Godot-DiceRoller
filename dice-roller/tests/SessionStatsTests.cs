using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class SessionStatsTests
{
    static RollOutcome Numeric(ulong id, HullKind hull, int value) =>
        new(id, hull, true, new FaceSlotId(value), FaceContent.Numeric(value));

    static RollOutcome Invalid(ulong id, HullKind hull) =>
        new(id, hull, false, FaceSlotId.None, FaceContent.None);

    [Fact]
    public void ByHull_groups_d6_count_sum_invalid_and_face_ids()
    {
        var outcomes = new[]
        {
            Numeric(1, HullKind.D6, 6),
            Numeric(2, HullKind.D6, 1),
            Invalid(3, HullKind.D6),
        };

        var groups = SessionStats.ByHull(outcomes);
        Assert.Single(groups);
        var d6 = groups[0];
        Assert.Equal(HullKind.D6, d6.Hull);
        Assert.Equal(3, d6.Count);
        Assert.Equal(7, d6.NumericSum);
        Assert.Equal(1, d6.InvalidCount);
        Assert.Contains(d6.Faces, f => f.Id == "6" && f.Occurrences == 1);
        Assert.Contains(d6.Faces, f => f.Id == "1" && f.Occurrences == 1);
    }

    [Fact]
    public void ByHull_mixed_hulls_are_separate_and_drop_when_empty()
    {
        var session = new DiceSessionState();
        session.Record(Numeric(1, HullKind.D6, 6));
        session.Record(Numeric(2, HullKind.D8, 8));

        var mixed = SessionStats.ByHull(session.Outcomes);
        Assert.Equal(2, mixed.Count);
        Assert.Equal(HullKind.D6, mixed[0].Hull);
        Assert.Equal(HullKind.D8, mixed[1].Hull);

        session.Forget(2);
        var remaining = SessionStats.ByHull(session.Outcomes);
        Assert.Single(remaining);
        Assert.Equal(HullKind.D6, remaining[0].Hull);
    }

    [Fact]
    public void ByHull_sword_counts_but_does_not_add_to_sum()
    {
        var outcomes = new[]
        {
            Numeric(1, HullKind.D6, 4),
            new RollOutcome(
                2,
                HullKind.D6,
                true,
                new FaceSlotId(6),
                new FaceContent { Id = "sword", Label = "Sword" }),
        };

        var d6 = Assert.Single(SessionStats.ByHull(outcomes));
        Assert.Equal(2, d6.Count);
        Assert.Equal(4, d6.NumericSum);
        Assert.Equal(0, d6.InvalidCount);
        Assert.Contains(d6.Faces, f => f.Id == "sword" && f.Occurrences == 1);
    }
}
