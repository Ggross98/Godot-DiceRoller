using DiceRoller.Core;
using Godot;
using Xunit;

namespace DiceRoller.Tests;

public class ArenaMathTests
{
    [Fact]
    public void NextAreaScale_size_up_from_default_adds_step()
    {
        Assert.Equal(0.22f, ArenaMath.NextAreaScale(0.2f, sizeUp: true, factor: 1f), 5);
    }

    [Fact]
    public void NextAreaScale_size_down_from_default_subtracts_step()
    {
        Assert.Equal(0.18f, ArenaMath.NextAreaScale(0.2f, sizeUp: false, factor: 1f), 5);
    }

    [Fact]
    public void NextAreaScale_at_max_size_up_falls_through_and_shrinks()
    {
        // Original: first branch requires scale < 1.4, so scroll-up at max still hits the elif and shrinks.
        Assert.Equal(1.38f, ArenaMath.NextAreaScale(1.4f, sizeUp: true, factor: 1f), 5);
    }

    [Fact]
    public void NextAreaScale_at_min_size_down_stays()
    {
        Assert.Equal(0.15f, ArenaMath.NextAreaScale(0.15f, sizeUp: false, factor: 1f), 5);
    }

    [Fact]
    public void ZoomCamera_in_shortens_when_above_min()
    {
        var start = new Vector3(0f, 7f, 2.7f);
        var zoomed = ArenaMath.ZoomCamera(start, zoomIn: true, zoomFactor: 1f);
        Assert.True(zoomed.Length() < start.Length());
        Assert.True(zoomed.Length() > ArenaMath.MinCameraDistance);
    }

    [Fact]
    public void ZoomCamera_out_lengthens_when_below_max()
    {
        var start = new Vector3(0f, 7f, 2.7f);
        var zoomed = ArenaMath.ZoomCamera(start, zoomIn: false, zoomFactor: 1f);
        Assert.True(zoomed.Length() > start.Length());
        Assert.True(zoomed.Length() < ArenaMath.MaxCameraDistance);
    }

    [Fact]
    public void ZoomCamera_in_at_min_distance_stays()
    {
        var start = Vector3.Forward * ArenaMath.MinCameraDistance;
        var zoomed = ArenaMath.ZoomCamera(start, zoomIn: true, zoomFactor: 1f);
        Assert.Equal(start, zoomed);
    }

    [Fact]
    public void ZoomCamera_out_at_max_distance_stays()
    {
        var start = Vector3.Forward * ArenaMath.MaxCameraDistance;
        var zoomed = ArenaMath.ZoomCamera(start, zoomIn: false, zoomFactor: 1f);
        Assert.Equal(start, zoomed);
    }

    [Fact]
    public void ScaledHalfExtent_default_floor_matches_original_int_cast()
    {
        Assert.Equal(5, ArenaMath.ScaledHalfExtent(25f, 0.2f));
    }
}
