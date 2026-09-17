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

    [Fact]
    public void Replace_raises_Changed()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var raised = 0;
        layout.Changed += () => raised++;

        layout.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clone_does_not_copy_Changed_subscribers()
    {
        var original = FaceLayouts.StandardNumeric(HullKind.D6);
        var originalRaised = 0;
        original.Changed += () => originalRaised++;

        var clone = original.Clone();
        var cloneRaised = 0;
        clone.Changed += () => cloneRaised++;

        clone.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        Assert.Equal(0, originalRaised);
        Assert.Equal(1, cloneRaised);
    }

    [Fact]
    public void TryGetSlotByContentId_finds_replaced_id()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        layout.Replace(new FaceSlotId(3), new FaceContent { Id = "fireball", Label = "Fireball" });

        Assert.True(layout.TryGetSlotByContentId("fireball", out var slot));
        Assert.Equal(3, slot.Value);
    }

    [Fact]
    public void TryGetSlotByContentId_returns_first_duplicate()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        layout.Replace(new FaceSlotId(2), new FaceContent { Id = "hit", Label = "A" });
        layout.Replace(new FaceSlotId(5), new FaceContent { Id = "hit", Label = "B" });

        Assert.True(layout.TryGetSlotByContentId("hit", out var slot));
        Assert.Equal(2, slot.Value);
    }

    [Fact]
    public void TryGetSlotByContentId_missing_id_is_false()
    {
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);

        Assert.False(layout.TryGetSlotByContentId("missing", out var slot));
        Assert.Equal(FaceSlotId.None, slot);
    }
}
