using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class FaceLayoutTests
{
    [Fact]
    public void StandardNumeric_d6_slot_6_is_numeric_six()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var content = layout[new FaceSlotId(6)];

        Assert.Equal("6", content.Id);
        Assert.Equal("6", content.Label);
        Assert.Equal(6, content.NumericValue);
    }

    [Fact]
    public void Replace_changes_content_for_that_slot_only()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        layout.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        Assert.Equal("sword", layout[new FaceSlotId(6)].Id);
        Assert.Equal("5", layout[new FaceSlotId(5)].Id);
    }

    [Fact]
    public void Clone_is_independent_of_source()
    {
        var original = FaceLayouts.StandardNumeric(HullKind.D6);
        var clone = original.Clone();
        clone.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        Assert.Equal("6", original[new FaceSlotId(6)].Id);
        Assert.Equal("sword", clone[new FaceSlotId(6)].Id);
    }
}
