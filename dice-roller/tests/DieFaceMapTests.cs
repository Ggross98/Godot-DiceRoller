using DiceRoller.Core;
using Godot;
using Xunit;

namespace DiceRoller.Tests;

public class DieFaceMapTests
{
    [Fact]
    public void Identity_d6_reads_slot_6()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var result = map.ReadUpSlot(Transform3D.Identity);

        Assert.True(result.IsValid);
        Assert.Equal(6, result.Slot.Value);
    }

    [Fact]
    public void Identity_d6_translated_below_origin_still_reads_slot_6()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var xf = new Transform3D(Basis.Identity, new Vector3(0f, -5f, 0f));
        var result = map.ReadUpSlot(xf);

        Assert.True(result.IsValid);
        Assert.Equal(6, result.Slot.Value);
    }

    [Fact]
    public void D6_tilted_45_degrees_around_x_is_invalid()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var tilt = new Transform3D(new Basis(Vector3.Right, Mathf.Pi / 4f), Vector3.Zero);
        var result = map.ReadUpSlot(tilt);

        Assert.False(result.IsValid);
        Assert.Equal(FaceSlotId.None, result.Slot);
    }

    [Fact]
    public void D6_slot_6_sample_is_up()
    {
        var map = DieFaceMap.For(HullKind.D6);
        Assert.Equal(Vector3.Up, map[new FaceSlotId(6)]);
    }

    [Fact]
    public void D4_slot_4_sample_is_negated_down()
    {
        var map = DieFaceMap.For(HullKind.D4);
        Assert.Equal(-Vector3.Down, map[new FaceSlotId(4)]);
    }

    [Theory]
    [InlineData(HullKind.D4, 1)]
    [InlineData(HullKind.D4, 4)]
    [InlineData(HullKind.D4, 3)]
    [InlineData(HullKind.D6, 1)]
    [InlineData(HullKind.D6, 6)]
    [InlineData(HullKind.D6, 4)]
    [InlineData(HullKind.D8, 1)]
    [InlineData(HullKind.D8, 4)]
    [InlineData(HullKind.D8, 8)]
    [InlineData(HullKind.D10, 1)]
    [InlineData(HullKind.D10, 10)]
    [InlineData(HullKind.D10, 5)]
    [InlineData(HullKind.D12, 1)]
    [InlineData(HullKind.D12, 12)]
    [InlineData(HullKind.D12, 6)]
    [InlineData(HullKind.D20, 1)]
    [InlineData(HullKind.D20, 10)]
    [InlineData(HullKind.D20, 20)]
    public void Aligning_sample_to_world_up_reads_that_slot(HullKind hull, int slotValue)
    {
        var map = DieFaceMap.For(hull);
        var slot = new FaceSlotId(slotValue);
        var xform = map.AlignSlotToWorldUp(Transform3D.Identity, slot);
        var result = map.ReadUpSlot(xform);

        Assert.True(result.IsValid);
        Assert.Equal(slot, result.Slot);
    }

    [Fact]
    public void AlignSlotToWorldUp_preserves_origin()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var origin = new Vector3(1.5f, -4f, 2.25f);
        var current = new Transform3D(new Basis(Vector3.Forward, 0.8f), origin);

        var aligned = map.AlignSlotToWorldUp(current, new FaceSlotId(1));

        Assert.Equal(origin, aligned.Origin);
    }

    [Theory]
    [InlineData(HullKind.D4, 1)]
    [InlineData(HullKind.D6, 1)]
    [InlineData(HullKind.D6, 3)]
    [InlineData(HullKind.D8, 8)]
    [InlineData(HullKind.D10, 5)]
    [InlineData(HullKind.D12, 12)]
    [InlineData(HullKind.D20, 20)]
    public void AlignSlotToWorldUp_from_arbitrary_basis_reads_that_slot(HullKind hull, int slotValue)
    {
        var map = DieFaceMap.For(hull);
        var slot = new FaceSlotId(slotValue);
        var current = new Transform3D(
            new Basis(Vector3.Up, 0.7f) * new Basis(Vector3.Right, 1.1f),
            new Vector3(3f, -2f, 4f));

        var aligned = map.AlignSlotToWorldUp(current, slot);
        var result = map.ReadUpSlot(aligned);

        Assert.Equal(current.Origin, aligned.Origin);
        Assert.True(result.IsValid);
        Assert.Equal(slot, result.Slot);
    }

    [Fact]
    public void AlignSlotToWorldUp_rejects_none()
    {
        var map = DieFaceMap.For(HullKind.D6);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => map.AlignSlotToWorldUp(Transform3D.Identity, FaceSlotId.None));
    }

    [Fact]
    public void AlignSlotToWorldUp_rejects_slot_missing_from_hull()
    {
        var map = DieFaceMap.For(HullKind.D6);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => map.AlignSlotToWorldUp(Transform3D.Identity, new FaceSlotId(20)));
    }
}
