using DiceRoller.Core;
using Godot;
using Xunit;

namespace DiceRoller.Tests;

public class DragMathTests
{
    [Fact]
    public void IntersectYPlane_horizontal_ray_returns_zero()
    {
        var hit = DragMath.IntersectYPlane(new Vector3(0, 10, 0), new Vector3(1, 0, 0), 1f);
        Assert.Equal(Vector3.Zero, hit);
    }

    [Fact]
    public void IntersectYPlane_from_above_hits_plane_y()
    {
        var hit = DragMath.IntersectYPlane(new Vector3(0, 10, 0), new Vector3(0, -1, 0), 1f);
        Assert.Equal(new Vector3(0, 1, 0), hit);
    }

    [Fact]
    public void ClampHorizontalThrow_zero_delta_is_zero()
    {
        var throwVec = DragMath.ClampHorizontalThrow(Vector3.Zero, 80f);
        Assert.Equal(Vector3.Zero, throwVec);
    }

    [Fact]
    public void ClampHorizontalThrow_long_xz_is_capped_at_max_with_zero_y()
    {
        var delta = new Vector3(10, 3, 0);
        var throwVec = DragMath.ClampHorizontalThrow(delta, 80f);

        Assert.Equal(0f, throwVec.Y);
        Assert.Equal(80f, throwVec.Length(), 3);
        Assert.True(throwVec.X > 0f);
    }

    [Fact]
    public void D6_identity_up_slot_sample_is_already_aligned_to_world_up()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var transform = Transform3D.Identity;
        var up = map.ReadUpSlot(transform);
        Assert.True(up.IsValid);

        var local = map[up.Slot];
        var origin = transform.Origin;
        var sideDir = ((transform * local) - origin).Normalized();
        var rotation = DragMath.RotationToAlignUp(sideDir);

        Assert.False(rotation.HasValue);
        Assert.True(sideDir.IsEqualApprox(Vector3.Up));
    }
}
