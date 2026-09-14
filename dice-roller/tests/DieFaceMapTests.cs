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
        var xform = AlignLocalToWorldUp(map[slot]);
        var result = map.ReadUpSlot(xform);

        Assert.True(result.IsValid);
        Assert.Equal(slot, result.Slot);
    }

    static Transform3D AlignLocalToWorldUp(Vector3 localSample)
    {
        var from = localSample.Normalized();
        var to = Vector3.Up;
        if (from.IsEqualApprox(to))
            return Transform3D.Identity;
        if (from.IsEqualApprox(-to))
            return new Transform3D(new Basis(Vector3.Right, Mathf.Pi), Vector3.Zero);

        var axis = from.Cross(to).Normalized();
        var angle = from.AngleTo(to);
        return new Transform3D(new Basis(axis, angle), Vector3.Zero);
    }
}
