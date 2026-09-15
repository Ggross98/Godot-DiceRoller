using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class NumericScoreTests
{
    static RollOutcome Numeric(ulong id, HullKind hull, int value, bool valid = true) =>
        new(
            id,
            hull,
            valid,
            valid ? new FaceSlotId(value) : FaceSlotId.None,
            valid ? FaceContent.Numeric(value) : FaceContent.None);

    [Fact]
    public void Sum_mixed_hulls_adds_numeric_values()
    {
        var latest = new[]
        {
            Numeric(1, HullKind.D6, 6),
            Numeric(2, HullKind.D8, 8),
        };

        Assert.Equal(14, NumericScore.Sum(latest));
    }

    [Fact]
    public void Sum_skips_invalid_and_non_numeric_content()
    {
        var latest = new[]
        {
            Numeric(1, HullKind.D6, 6),
            Numeric(2, HullKind.D6, 1, valid: false),
            new RollOutcome(
                3,
                HullKind.D6,
                true,
                new FaceSlotId(6),
                new FaceContent { Id = "sword", Label = "Sword" }),
        };

        Assert.Equal(6, NumericScore.Sum(latest));
    }
}
