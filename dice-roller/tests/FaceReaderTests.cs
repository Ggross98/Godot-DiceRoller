using DiceRoller.Core;
using Godot;
using Xunit;

namespace DiceRoller.Tests;

public class FaceReaderTests
{
    [Fact]
    public void Identity_d6_standard_layout_reads_numeric_six()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var outcome = FaceReader.Read(map, layout, Transform3D.Identity);

        Assert.True(outcome.IsValid);
        Assert.Equal(6, outcome.Slot.Value);
        Assert.Equal(6, outcome.Content.NumericValue);
        Assert.Equal(HullKind.D6, outcome.Hull);
    }

    [Fact]
    public void Replace_changes_content_id_but_not_slot()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        layout.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        var outcome = FaceReader.Read(map, layout, Transform3D.Identity);

        Assert.True(outcome.IsValid);
        Assert.Equal(6, outcome.Slot.Value);
        Assert.Equal("sword", outcome.Content.Id);
        Assert.Null(outcome.Content.NumericValue);
    }

    [Fact]
    public void Invalid_pose_uses_none_content()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var tilt = new Transform3D(new Basis(Vector3.Right, Mathf.Pi / 4f), Vector3.Zero);

        var outcome = FaceReader.Read(map, layout, tilt);

        Assert.False(outcome.IsValid);
        Assert.Equal(FaceSlotId.None, outcome.Slot);
        Assert.Same(FaceContent.None, outcome.Content);
    }

    [Fact]
    public void Upright_d6_below_world_origin_still_reads_numeric_six()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var xf = new Transform3D(Basis.Identity, new Vector3(0f, -5f, 0f));

        var outcome = FaceReader.Read(map, layout, xf);

        Assert.True(outcome.IsValid);
        Assert.Equal(6, outcome.Slot.Value);
        Assert.Equal(6, outcome.Content.NumericValue);
    }

    [Fact]
    public void Aligning_replaced_slot_reads_new_content_id()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        layout.Replace(new FaceSlotId(1), new FaceContent { Id = "sword", Label = "Sword" });
        var start = new Transform3D(
            new Basis(Vector3.Up, 0.7f) * new Basis(Vector3.Right, 1.1f),
            new Vector3(3f, -2f, 4f));

        var aligned = map.AlignSlotToWorldUp(start, new FaceSlotId(1));
        var outcome = FaceReader.Read(map, layout, aligned);

        Assert.True(outcome.IsValid);
        Assert.Equal(1, outcome.Slot.Value);
        Assert.Equal("sword", outcome.Content.Id);
    }
}
